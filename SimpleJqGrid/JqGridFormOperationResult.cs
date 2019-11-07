using System.Web.Mvc;

namespace SimpleJqGrid
{
	public class JqGridFormOperationResult : JsonResult
	{
		/// <summary>
		/// Create form operation result
		/// </summary>
		/// <param name="success">Defined operation was success</param>
		/// <param name="message">Set message for not success result</param>
		public JqGridFormOperationResult(bool success, string message = null, object rowid = null)
		{
			Data = new { success, message, rowid };
		}

		/// <summary>
		/// Create form operation result with success response
		/// </summary>
		public JqGridFormOperationResult() : this(true) { }

		/// <summary>
		/// Create form operation result with success response
		/// </summary>
		public JqGridFormOperationResult(object rowid) : this(true, rowid: rowid) { }

		/// <summary>
		/// Create form operation result with not success response and message
		/// </summary>
		/// <param name="message">Set message for not success result</param>

		public JqGridFormOperationResult(string message) : this(false, message) { }
	}
}