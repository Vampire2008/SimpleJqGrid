using System.Web.Mvc;
using Lib.Web.Mvc.JqGridFork;

namespace SimpleJqGrid
{
	public abstract class JqGridResultBase : ActionResult
	{
		/// <summary>
		/// Enable paging server-side function
		/// </summary>
		public bool IsPagingEnabled { get; set; }
		/// <summary>
		/// Enable sorting server-side function
		/// </summary>
		public bool IsSortingEnabled { get; set; }
		/// <summary>
		/// Enable add form response, it can send message when operation is not success. Required set AfterSubmit function for add operation in JqGrid
		/// </summary>
		public bool EnableAddJsonResponse { get; set; }
		/// <summary>
		/// Enable edit form response, it can send message when operation is not success. Required set AfterSubmit function for edit operation in JqGrid
		/// </summary>
		public bool EnableEditJsonResponse { get; set; }
		/// <summary>
		/// Enable delete form response, it can send message when operation is not success. Required set AfterSubmit function for delete operation in JqGrid
		/// </summary>
		public bool EnableDeleteJsonResponse { get; set; }

		/// <summary>
		/// Message displayed when edit operation is not success, also used as default message for Add and Delete operations when they are not defined
		/// </summary>
		public string EditErrorMessage { get; set; }
		/// <summary>
		/// Message displayed when add operation is not success
		/// </summary>
		public string AddErrorMessage { get; set; }
		/// <summary>
		/// Message displayed when delete operation is not success
		/// </summary>
		public string DeleteErrorMessage { get; set; }

		public JqGridParametersNames ParametersNames { get; set; } = JqGridRequest.ParameterNames;
		public JqGridJsonReader JsonReader { get; set; } = JqGridResponse.JsonReader;
		public JsonRequestBehavior JsonRequestBehavior { get; set; } = JsonRequestBehavior.DenyGet;

		protected string GetFilter(string searchingName, JqGridSearchOperators searchingOperator, string searchingValue)
		{
			var searchingOperatorString = string.Empty;
			switch (searchingOperator)
			{
				case JqGridSearchOperators.Eq:
					searchingOperatorString = "=";
					break;
				case JqGridSearchOperators.Ne:
					searchingOperatorString = "!=";
					break;
				case JqGridSearchOperators.Lt:
					searchingOperatorString = "<";
					break;
				case JqGridSearchOperators.Le:
					searchingOperatorString = "<=";
					break;
				case JqGridSearchOperators.Gt:
					searchingOperatorString = ">";
					break;
				case JqGridSearchOperators.Ge:
					searchingOperatorString = ">=";
					break;
			}

			searchingName = searchingName.Replace("Category", "CategoryId");
			if (searchingName == "Id" || searchingName == "SupplierId" || searchingName == "CategoryId")
				return $"{searchingName} {searchingOperatorString} {searchingValue}";

			if (searchingName == "Name")
			{
				switch (searchingOperator)
				{
					case JqGridSearchOperators.Bw:
						return $"{searchingName}.StartsWith(\"{searchingValue}\")";
					case JqGridSearchOperators.Bn:
						return $"!{searchingName}.StartsWith(\"{searchingValue}\")";
					case JqGridSearchOperators.Ew:
						return $"{searchingName}.EndsWith(\"{searchingValue}\")";
					case JqGridSearchOperators.En:
						return $"!{searchingName}.EndsWith(\"{searchingValue}\")";
					case JqGridSearchOperators.Cn:
						return $"{searchingName}.Contains(\"{searchingValue}\")";
					case JqGridSearchOperators.Nc:
						return $"!{searchingName}.Contains(\"{searchingValue}\")";
					default:
						return $"{searchingName} {searchingOperatorString} \"{searchingValue}\"";
				}
			}

			return string.Empty;
		}

		protected void InitFormOptions()
		{
			if (string.IsNullOrWhiteSpace(EditErrorMessage))
				EditErrorMessage = "Error on operation";
		}
	}
}