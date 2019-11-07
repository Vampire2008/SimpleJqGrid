using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web.Mvc;
using Lib.Web.Mvc.JqGridFork;
using Lib.Web.Mvc.JqGridFork.DataAnnotations;
using SimpleJqGrid.Exceptions;
using SimpleJqGrid;

namespace SimpleJqGrid
{

	/// <inheritdoc />
	/// <summary>
	/// Action result class for work with JqGrid with minimal custom logic. Can work as with IEnumerable and as with SharpRepository
	/// </summary>
	/// <typeparam name="T">View model that implement <see cref="T:Lib.Web.Mvc.SimpleJqGrid.IJqGridRow`2" /></typeparam>
	/// <typeparam name="TKey">Type of identity in source data model</typeparam>
	public class SimpleJqGridResult<T, TKey> : JqGridResultBase
        where T : ISimpleJqGridRow<TKey>, new()
    {
        private IQueryable<T> _rows;
        private readonly Func<T, OperationResult> _addFunc;
        private readonly Func<T, OperationResult> _editFunc;
        private readonly Func<TKey, OperationResult> _delFunc;
        private readonly Expression<Func<T, bool>> _additionalSelector;



        /// <summary>
        /// Initialize a new instance of the JqGridResult using <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <param name="model">Data source</param>
        /// <param name="addFunc">Custom function that add data from JqGrid</param>
        /// <param name="editFunc">Custom function that edit data from JqGrid</param>
        /// <param name="delFunc">Custom function that delete data from JqGrid</param>
        /// <param name="additionalSelector">Additional selector for repository before other functions</param>
        public SimpleJqGridResult(IEnumerable<T> model,
            Func<T, OperationResult> addFunc = null,
            Func<T, OperationResult> editFunc = null,
            Func<TKey, OperationResult> delFunc = null,
            Expression<Func<T, bool>> additionalSelector = null)
        {
            _rows = model.AsQueryable();
            _addFunc = addFunc;
            _editFunc = editFunc;
            _delFunc = delFunc;
            _additionalSelector = additionalSelector;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (ParametersNames == null) ParametersNames = JqGridRequest.ParameterNames;

            var oper = context.Controller.ValueProvider.GetValue(ParametersNames.Operator)?.AttemptedValue;

            if (oper == null)
            {
                var requestModel = new JqGridRequest();
                var requestModelState = Helpers.BindModel(ref requestModel, context);
                if (!requestModelState.IsValid)
                    throw new JqGridModelBindingException("Error binding JqGrid request model", requestModelState);

                var filterExpression = string.Empty;
                if (requestModel.Searching)
                {
                    if (requestModel.SearchingFilter != null)
                        filterExpression = GetFilter(requestModel.SearchingFilter.SearchingName,
                            requestModel.SearchingFilter.SearchingOperator, requestModel.SearchingFilter.SearchingValue);
                    else if (requestModel.SearchingFilters != null)
                    {
                        var filterExpressionBuilder = new StringBuilder();
                        var groupingOperatorToString = requestModel.SearchingFilters.GroupingOperator.ToString();
                        foreach (var searchingFilter in requestModel.SearchingFilters.Filters)
                        {
                            filterExpressionBuilder.Append(GetFilter(searchingFilter.SearchingName, searchingFilter.SearchingOperator,
                                searchingFilter.SearchingValue));
                            filterExpressionBuilder.Append($" {groupingOperatorToString} ");
                        }

                        if (filterExpressionBuilder.Length > 0)
                            filterExpressionBuilder.Remove(filterExpressionBuilder.Length - groupingOperatorToString.Length - 2,
                                groupingOperatorToString.Length + 2);
                        filterExpression = filterExpressionBuilder.ToString();
                    }
                    else
                    {
                        var filterExpressionBuilder = new StringBuilder();
                        foreach (var property in typeof(T).GetProperties().Where(p =>
                        {
                            var attrs = p.GetCustomAttributes<JqGridColumnSearchableAttribute>(false);
                            return !attrs.Any() || attrs.Any(a => a.Searchable);
                        }))
                        {
                            var value = context.Controller.ValueProvider.GetValue(property.Name)?.AttemptedValue;
                            if (value == null) continue;
                            filterExpressionBuilder.Append(property.PropertyType == typeof(string)
                                ? $"{property.Name} = \"{value}\" AND "
                                : $"{property.Name} = {value} AND ");
                            if (filterExpressionBuilder.Length > 0)
                                filterExpressionBuilder.Remove(filterExpressionBuilder.Length - 5, 5);
                            filterExpression = filterExpressionBuilder.ToString();
                        }
                    }
                }


                var totalRecords = _rows.Count();
                if (_additionalSelector != null)
                {
                    _rows = _rows.Where(_additionalSelector);
                }

                if (!string.IsNullOrEmpty(filterExpression))
                {
                    _rows = _rows.Where(filterExpression);
                }

                if (IsSortingEnabled)
                {
                    if (!string.IsNullOrWhiteSpace(requestModel.SortingName))
                    {
                        _rows = _rows.OrderBy($"{requestModel.SortingName} {requestModel.SortingOrder}");
                    }
                    else
                    {
                        var props = typeof(T).GetProperties();
                        if (props.Length == 0)
                        {
                            throw new NoGetPropertiesException();
                        }

                        _rows = _rows.OrderBy($"{props[0].Name} {requestModel.SortingOrder}");
                    }
                }

                if (IsPagingEnabled)
                {
                    _rows = _rows
                        .Skip(requestModel.PageIndex * requestModel.RecordsCount)
                        .Take(requestModel.PagesCount ?? 1 * requestModel.RecordsCount);
                }

                var response = new JqGridResponse
                {
                    TotalPagesCount = (int)Math.Ceiling((float)totalRecords / requestModel.RecordsCount),
                    PageIndex = requestModel.PageIndex,
                    TotalRecordsCount = totalRecords,
                    Reader = JsonReader ?? JqGridResponse.JsonReader
                };
                response.Records.AddRange(_rows.ToList().Select(r => new JqGridRecord(r.Id.ToString(), r)));
                new JqGridJsonResult { Data = response, JsonRequestBehavior = JsonRequestBehavior }.ExecuteResult(context);
            }
            else if (oper == ParametersNames.AddOperator)
            {
                if (_addFunc == null)
                    throw new FunctionMissedException("No add function");
                var rowModel = new T();
                var rowModelState = Helpers.BindModel(ref rowModel, context);
                if (!rowModelState.IsValid)
                    throw new JqGridModelBindingException("Error binding row model", rowModelState);

                var result = _addFunc(rowModel);
                if (result?.Success ?? true)
                {
                    new JqGridFormOperationResult(rowModel.Id)
                    {
                        JsonRequestBehavior = JsonRequestBehavior
                    }.ExecuteResult(context);
                }
                else
                {
                    if (EnableAddJsonResponse)
                    {
                        InitFormOptions();
                        new JqGridFormOperationResult(result.Message ?? AddErrorMessage ?? EditErrorMessage)
                        {
                            JsonRequestBehavior = JsonRequestBehavior
                        }.ExecuteResult(context);
                    }
                    else
                    {
                        new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                            (result.Message ?? AddErrorMessage ??
                             EditErrorMessage).Replace("\n", ""))
                            .ExecuteResult(context);
                    }
                }
            }
            else if (oper == ParametersNames.EditOperator)
            {
                if (_editFunc == null)
                    throw new FunctionMissedException("No edit function");
                var editRowModel = new T();
                var editRowModelState = Helpers.BindModel(ref editRowModel, context);
                if (!editRowModelState.IsValid)
                    throw new JqGridModelBindingException("Error binding row model", editRowModelState);

                var result = _editFunc(editRowModel);
                if (result?.Success ?? true)
                {
                    if (EnableEditJsonResponse)
                    {
                        new JqGridFormOperationResult
                        {
                            JsonRequestBehavior = JsonRequestBehavior
                        }.ExecuteResult(context);
                    }
                    else
                    {
                        new HttpStatusCodeResult(HttpStatusCode.OK).ExecuteResult(context);
                    }
                }
                else
                {
                    if (EnableEditJsonResponse)
                    {
                        InitFormOptions();
                        new JqGridFormOperationResult(result.Message ?? EditErrorMessage)
                        {
                            JsonRequestBehavior = JsonRequestBehavior
                        }.ExecuteResult(context);
                    }
                    else
                    {
                        new HttpStatusCodeResult(HttpStatusCode.BadRequest, (result.Message ?? EditErrorMessage).Replace("\n", ""))
                            .ExecuteResult(context);
                    }
                }
            }
            else if (oper == ParametersNames.DeleteOperator)
            {
                if (_delFunc == null)
                    throw new FunctionMissedException("No delete function");
                var delRowModel = new T();
                var delRowModelState = Helpers.BindModel(ref delRowModel, context);
                if (!delRowModelState.IsValid)
                    throw new JqGridModelBindingException("Error binding row model", delRowModelState);

                var result = _delFunc(delRowModel.Id);
                if (result?.Success ?? true)
                {
                    if (EnableDeleteJsonResponse)
                    {
                        new JqGridFormOperationResult
                        {
                            JsonRequestBehavior = JsonRequestBehavior
                        }.ExecuteResult(context);
                    }
                    else
                    {
                        new HttpStatusCodeResult(HttpStatusCode.OK).ExecuteResult(context);
                    }
                }
                else
                {
                    if (EnableDeleteJsonResponse)
                    {
                        InitFormOptions();
                        new JqGridFormOperationResult(result.Message ?? DeleteErrorMessage ?? EditErrorMessage)
                        {
                            JsonRequestBehavior = JsonRequestBehavior
                        }.ExecuteResult(context);
                    }
                    else
                    {
                        new HttpStatusCodeResult(HttpStatusCode.BadRequest, (result.Message ?? DeleteErrorMessage ?? EditErrorMessage).Replace("\n", ""))
                            .ExecuteResult(context);
                    }
                }
            }
            else
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}