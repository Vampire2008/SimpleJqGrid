using System;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Lib.Web.Mvc.JqGridFork;

namespace SimpleJqGrid
{
	public static class JqGridHelperExtension
	{
		public const string InlineAfterSaveFunction = "extensionAfterSave";
		public const string InlineSuccessFunction = "extensionSuccessFunction";
		public const string FormAfterSubmitFunction = "extensionAfterSubmit";

		public static string GetFormAfterSubmitFunction(string gridId, string prefix = "")
		{
			return $"{gridId}{prefix}_{FormAfterSubmitFunction}";
		}

		public static string GetInlineAfterSaveFunction(string gridId, string prefix = "")
		{
			return $"{gridId}{prefix}_{InlineAfterSaveFunction}";
		}

		public static string GetInlineSuccessFunction(string gridId, string prefix = "")
		{
			return $"{gridId}{prefix}_{InlineSuccessFunction}";
		}

		public static MvcHtmlString GetJavaScriptInlineFunctions<TModel>(this JqGridHelper<TModel> grid, string prefix = "", string customFunctionAfterSave = null, string customSuccessFunction = null)
		{
			var result = new StringBuilder();
			result.AppendFormat("function {0}(rowid,response) {{\n", GetInlineAfterSaveFunction(grid.Id, prefix))
			.AppendLine(@"	let data;
	try {
		data = JSON.parse(response.responseText);
		if (data.rowid)
			$(`#${rowid}`).attr(""id"", data.rowid);
	}
	catch (e) {
		return;
	}");
			if (!string.IsNullOrWhiteSpace(customFunctionAfterSave))
			{
				result.AppendFormat("\t{0}.call(this,rowid,response);\n", customFunctionAfterSave);
			}
			result.AppendLine("}")
				.AppendLine()
				.AppendFormat("function {0}(response) {{\n", GetInlineSuccessFunction(grid.Id, prefix))
				.AppendLine(@"	let data, result = false;
	try {
		data = JSON.parse(response.responseText);
		if (data.success) {
			result = true;
		} else {
			if (data.message)
				alert(data.message);
			$(""#Grid"").jqGrid('showAddEditButtons');
		}
	}
	catch (e) {
		result = false;
	}");
			if (!string.IsNullOrWhiteSpace(customSuccessFunction))
			{
				result.AppendFormat("\tconst sucret = {0}.call(this,response,result);\n", customSuccessFunction)
					.AppendLine("\treturn sucret || result;");
			}
			else
			{
				result.AppendLine("\treturn result;");
			}
			result.Append("}");
			if (!HttpContext.Current.IsDebuggingEnabled)
			{
				result.Replace("\n", "").Replace("\t", "");
			}
			return new MvcHtmlString(result.ToString());
		}

		public static MvcHtmlString GetJavaScriptFormAfterSubmitFunction(this IJqGridHelper grid, string prefix = "", string customAfterSubmitFunction = null)
		{
			var result = new StringBuilder();
			result.AppendFormat("function {0}(response, postdata, oper) {{\n", GetFormAfterSubmitFunction(grid.Id, prefix));
			result.AppendLine(@"	let data,result;
	try {
		data = JSON.parse(response.responseText);
	}
	catch (e)
	{
		return [false,""Wrong answer from server""];
	}
	if (data.success) {
		if (oper === ""add"")
			result = [true, null, data.rowid];
		else
			result = [true];
	} else {
		result = [false, data.message];
	}");
			if (!string.IsNullOrWhiteSpace(customAfterSubmitFunction))
			{
				result.AppendFormat("\tconst custres = {0}.call(this,response, postdata, oper, Object.assign([], result));\n", customAfterSubmitFunction)
					.AppendLine("\treturn custres || result;");
			}
			else
			{
				result.AppendLine("\treturn result;");
			}
			result.Append("}");
			if (!HttpContext.Current.IsDebuggingEnabled)
			{
				result.Replace("\n", "").Replace("\t", "");
			}
			return new MvcHtmlString(result.ToString());
		}

		public static JqGridHelper<T> UseFormEditingScripts<T>(this JqGridHelper<T> helper, bool add = true, string addPrefix = "", bool edit = true, string editPrefix = "", bool delete = true, string deletePrefix = "")
		{
			if (helper.NavigatorOptions == null)
				throw new NullReferenceException("Navigator must be enabled");
			if (add)
			{
				if (helper.NavigatorAddOptions == null)
					throw new NullReferenceException("Add action options must be not null");
				helper.NavigatorAddOptions.AfterSubmit = GetFormAfterSubmitFunction(helper.Id, addPrefix);
			}
			if (edit)
			{
				if (helper.NavigatorEditOptions == null)
					throw new NullReferenceException("Edit action options must be not null");
				helper.NavigatorEditOptions.AfterSubmit = GetFormAfterSubmitFunction(helper.Id, editPrefix);
			}
			if (delete)
			{
				if (helper.NavigatorDeleteOptions == null)
					throw new NullReferenceException("Delete action options must be not null");
				helper.NavigatorDeleteOptions.AfterSubmit = GetFormAfterSubmitFunction(helper.Id, deletePrefix);
			}
			//helper.NavigatorAddOptions
			return helper;
		}

		public static JqGridHelper<T> UseInlineEditingScripts<T>(this JqGridHelper<T> helper, bool add = true, string addPreifx = "", bool edit = true, string editPrefix = "")
		{
			if (helper.InlineNavigatorOptions == null)
				throw new NullReferenceException("Inline navigator must be enabled");
			if (add)
			{
				if (helper.InlineNavigatorOptions.AddActionOptions == null)
					helper.InlineNavigatorOptions.AddActionOptions = new JqGridInlineNavigatorAddActionOptions();
				if (helper.InlineNavigatorOptions.AddActionOptions.Options == null)
					helper.InlineNavigatorOptions.AddActionOptions.Options = new JqGridInlineNavigatorActionOptions();
				helper.InlineNavigatorOptions.AddActionOptions.Options.AfterSaveFunction = GetInlineAfterSaveFunction(helper.Id, addPreifx);
				helper.InlineNavigatorOptions.AddActionOptions.Options.SuccessFunction = GetInlineSuccessFunction(helper.Id, addPreifx);
			}
			if (edit)
			{
				if (helper.InlineNavigatorOptions.ActionOptions == null)
					helper.InlineNavigatorOptions.ActionOptions = new JqGridInlineNavigatorActionOptions();
				helper.InlineNavigatorOptions.ActionOptions.AfterSaveFunction = GetInlineAfterSaveFunction(helper.Id, editPrefix);
				helper.InlineNavigatorOptions.ActionOptions.SuccessFunction = GetInlineSuccessFunction(helper.Id, editPrefix);
			}
			return helper;
		}
	}
}