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
using SharpRepository.Repository;
using SharpRepository.Repository.Queries;
using SharpRepository.Repository.Specifications;
using SimpleJqGrid;

namespace SimpleJqGrid
{
	/// <summary>
	/// Action result class for work with JqGrid with minimal custom logic. Can work as with IEnumerable and as with SharpRepository
	/// </summary>
	/// <remarks>Type of identity column in source data model is <see cref="int"/></remarks>
	/// <typeparam name="T">View model that implement <see cref="IJqGridRow{T,TKey}"/></typeparam>
	/// <typeparam name="TModel">Source data model</typeparam>
	public class JqGridResult<T, TModel> : JqGridResult<T, TModel, int>
		where T : IJqGridRow<TModel, int>, new()
		where TModel : class, new()
	{
		public JqGridResult(IEnumerable<TModel> model, Func<TModel, OperationResult> addFunc = null, Func<TModel, int> getIdFunc = null, Func<TModel, OperationResult> editFunc = null, Func<IEnumerable<TModel>, int, TModel> findFunc = null, Func<int, OperationResult> delFunc = null, Expression<Func<TModel, bool>> additionalSelector = null)
			: base(model, addFunc, getIdFunc, editFunc, findFunc, delFunc, additionalSelector)
		{

		}

		public JqGridResult(IRepository<TModel, int> repository, Func<TModel, OperationResult> addFunc = null, Func<TModel, int> getIdFunc = null, Func<TModel, OperationResult> editFunc = null, Func<int, OperationResult> delFunc = null, ISpecification<TModel> specification = null)
			: base(repository, addFunc, getIdFunc, editFunc, delFunc, specification)
		{
		}
	}


	/// <inheritdoc />
	/// <summary>
	/// Action result class for work with JqGrid with minimal custom logic. Can work as with IEnumerable and as with SharpRepository
	/// </summary>
	/// <typeparam name="T">View model that implement <see cref="T:Lib.Web.Mvc.SimpleJqGrid.IJqGridRow`2" /></typeparam>
	/// <typeparam name="TModel">Source data model</typeparam>
	/// <typeparam name="TKey">Type of identity in source data model</typeparam>
	public class JqGridResult<T, TModel, TKey> : JqGridResultBase
		where T : IJqGridRow<TModel, TKey>, new()
		where TModel : class, new()
	{
		private readonly IRepository<TModel, TKey> _repository;
		private ISpecification<TModel> _specification;
		private IQueryable<TModel> _rows;
		private readonly Func<TModel, TKey> _getIdFunc;
		private readonly Func<TModel, OperationResult> _addFunc;
		private readonly Func<TModel, OperationResult> _editFunc;
		private readonly Func<TKey, OperationResult> _delFunc;
		private readonly Expression<Func<TModel, bool>> _additionalSelector;
		private readonly Func<IEnumerable<TModel>, TKey, TModel> _findFunc;


		/// <summary>
		/// Initialize a new instance of the JqGridResult using <see cref="IEnumerable{TModel}"/>
		/// </summary>
		/// <param name="model">Data source</param>
		/// <param name="addFunc">Custom function that add data from JqGrid</param>
		/// <param name="getIdFunc">Custom function that return Id field of TModel (used for return Id after add operation) (required for add operation)</param>
		/// <param name="editFunc">Custom function that edit data from JqGrid</param>
		/// <param name="findFunc">Required for edit. Find object for edit in collection.</param>
		/// <param name="delFunc">Custom function that delete data from JqGrid</param>
		/// <param name="additionalSelector">Additional selector for repository before other functions</param>
		public JqGridResult(IEnumerable<TModel> model,
							Func<TModel, OperationResult> addFunc = null,
							Func<TModel, TKey> getIdFunc = null,
							Func<TModel, OperationResult> editFunc = null,
							Func<IEnumerable<TModel>, TKey, TModel> findFunc = null,
							Func<TKey, OperationResult> delFunc = null,
							Expression<Func<TModel, bool>> additionalSelector = null)
		{
			_rows = model.AsQueryable();
			_addFunc = addFunc;
			_getIdFunc = getIdFunc;
			_editFunc = editFunc;
			_findFunc = findFunc;
			_delFunc = delFunc;
			_additionalSelector = additionalSelector;
		}

		/// <summary>
		/// Initialize a new instance of the JqGridResult using <see cref="IRepository{TModel,TKey}"/>
		/// </summary>
		/// <param name="repository">Source repository</param>
		/// <param name="addFunc">Custom function that add data from JqGrid</param>
		/// <param name="getIdFunc">Custom function that return Id field of TModel (used for return Id after add operation)</param>
		/// <param name="editFunc">Custom function that edit data from JqGrid</param>
		/// <param name="delFunc">Custom function that delete data from JqGrid</param>
		/// <param name="specification">Add <see cref="ISpecification{T}"/> to selector</param>
		public JqGridResult(IRepository<TModel, TKey> repository,
			Func<TModel, OperationResult> addFunc = null,
			Func<TModel, TKey> getIdFunc = null,
			Func<TModel, OperationResult> editFunc = null,
			Func<TKey, OperationResult> delFunc = null,
			ISpecification<TModel> specification = null)
		{
			_repository = repository;
			if (addFunc != null)
				_addFunc = addFunc;
			else
				_addFunc = model =>
				{
					_repository.Add(model);
					return true;
				};
			if (getIdFunc != null)
				_getIdFunc = getIdFunc;
			else
				_getIdFunc = model =>
				{
					return _repository.GetPrimaryKey(model);
				};
			if (editFunc != null)
				_editFunc = editFunc;
			else
				_editFunc = model =>
				{
					_repository.Update(model);
					return true;
				};
			if (delFunc != null)
				_delFunc = delFunc;
			else
				_delFunc = key =>
				{
					_repository.Delete(key);
					return true;
				};
			_specification = specification;
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
				var backedParamNames = JqGridRequest.ParameterNames;
				JqGridRequest.ParameterNames = ParametersNames;
				var requestModelState = Helpers.BindModel(ref requestModel, context);
				JqGridRequest.ParameterNames = backedParamNames;
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

				int totalRecords;


				if (_repository != null)
				{
					if (!string.IsNullOrEmpty(filterExpression))
					{
						if (_specification != null)
						{
							if (_specification.Predicate != null)
								_specification = _specification.And(DynamicExpression.ParseLambda<TModel, bool>(filterExpression));
							else
							{
								var newSpecification =
									new Specification<TModel>(DynamicExpression.ParseLambda<TModel, bool>(filterExpression))
									{
										FetchStrategy = _specification.FetchStrategy
									};
								_specification = newSpecification;
							}
						}
						else
							_specification = new Specification<TModel>(DynamicExpression.ParseLambda<TModel, bool>(filterExpression));
					}

					IQueryOptions<TModel> options = null;
					if (IsPagingEnabled && requestModel.RecordsCount >= 0)
					{
						string sortName;
						if (!string.IsNullOrWhiteSpace(requestModel.SortingName))
						{
							sortName = requestModel.SortingName;
						}
						else
						{
							var props = typeof(TModel).GetProperties();
							if (props.Length == 0)
							{
								throw new NoGetPropertiesException();
							}

							sortName = props[0].Name;
						}
						options = new PagingOptions<TModel>(requestModel.PageIndex + 1, requestModel.RecordsCount,
							sortName, requestModel.SortingOrder == JqGridSortingOrders.Desc);
					}
					else if (IsSortingEnabled)
					{
						if (!string.IsNullOrWhiteSpace(requestModel.SortingName))
							options = new SortingOptions<TModel>(requestModel.SortingName,
								requestModel.SortingOrder == JqGridSortingOrders.Desc);
					}

					if (_specification != null && _specification.Predicate == null)
					{
						var newSpecification = new Specification<TModel>(model => true)
						{
							FetchStrategy = _specification.FetchStrategy
						};
						_specification = newSpecification;
					}
					_rows = _repository.FindAll(_specification, options).AsQueryable();
					totalRecords = _repository.Count(_specification);
				}
				else
				{
					totalRecords = _rows.Count();
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
							var props = typeof(TModel).GetProperties();
							if (props.Length == 0)
							{
								throw new NoGetPropertiesException();
							}

							_rows = _rows.OrderBy($"{props[0].Name} {requestModel.SortingOrder}");
						}
					}

					if (IsPagingEnabled && requestModel.RecordsCount >= 0)
					{
						_rows = _rows
							.Skip(requestModel.PageIndex * requestModel.RecordsCount)
							.Take(requestModel.PagesCount ?? 1 * requestModel.RecordsCount);
					}

				}

				var response = new JqGridResponse
				{
					TotalPagesCount = (int)Math.Ceiling((float)totalRecords / requestModel.RecordsCount),
					PageIndex = requestModel.PageIndex,
					TotalRecordsCount = totalRecords,
					Reader = JsonReader ?? JqGridResponse.JsonReader
				};
				response.Records.AddRange(_rows.ToList().Select(r =>
				{
					var t = new T();
					t.CopyFrom(r);
					return new JqGridRecord(t.Id.ToString(), t);
				}));
				new JqGridJsonResult { Data = response, JsonRequestBehavior = JsonRequestBehavior }.ExecuteResult(context);
			}
			else if (oper == ParametersNames.AddOperator)
			{
				if (_addFunc == null)
					throw new FunctionMissedException("No repository or add function");
				var rowModel = new T();
				var rowModelState = Helpers.BindModel(ref rowModel, context);
				if (!rowModelState.IsValid)
				{
					var needThrow = false;
					foreach (var modelState in rowModelState)
					{
						if (modelState.Key == nameof(ISimpleJqGridRow<int>.Id)) continue;
						if (!modelState.Value.Errors.Any()) continue;
						needThrow = true;
						break;
					}
					if (needThrow)
						throw new JqGridModelBindingException("Error binding row model", rowModelState);
				}

				var addModel = new TModel();
				rowModel.ApplyTo(addModel);
				var result = _addFunc(addModel);
				if (result?.Success ?? true)
				{
					object rowId = null;
					if (_getIdFunc != null) rowId = _getIdFunc(addModel);
					new JqGridFormOperationResult(rowId)
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
						new HttpStatusCodeResult(HttpStatusCode.BadRequest, (result.Message ?? AddErrorMessage ?? EditErrorMessage).Replace("\n", "")).ExecuteResult(context);
					}
				}
			}
			else if (oper == ParametersNames.EditOperator)
			{
				if (_editFunc == null)
					throw new FunctionMissedException("No repository or edit function");
				var editRowModel = new T();
				var editRowModelState = Helpers.BindModel(ref editRowModel, context);
				if (!editRowModelState.IsValid)
					throw new JqGridModelBindingException("Error binding row model", editRowModelState);

				TModel editModel;
				if (_repository != null)
				{
					editModel = _repository.Get(editRowModel.Id, _specification?.FetchStrategy);
				}
				else if (_findFunc != null)
				{
					editModel = _findFunc(_rows, editRowModel.Id);
				}
				else
				{
					throw new FunctionMissedException("To edit in no repository mode need find function.");
				}
				editRowModel.ApplyTo(editModel);
				var result = _editFunc(editModel);
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
						context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
						new JqGridFormOperationResult(result.Message ?? EditErrorMessage)
						{
							JsonRequestBehavior = JsonRequestBehavior
						}.ExecuteResult(context);
					}
					else
					{
						context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
						context.HttpContext.Response.Write((result.Message ?? EditErrorMessage).Replace("\n", ""));
					}
				}
			}
			else if (oper == ParametersNames.DeleteOperator)
			{
				if (_delFunc == null)
					throw new FunctionMissedException("No repository or edit function");
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
						context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
						context.HttpContext.Response.Write((result.Message ?? DeleteErrorMessage ?? EditErrorMessage).Replace("\n", ""));
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