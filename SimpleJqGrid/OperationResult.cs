namespace SimpleJqGrid
{
	public class OperationResult
	{
		public bool Success { get; set; } = true;
		public string Message { get; set; }

		/// <summary>
		/// Create success OperationResult
		/// </summary>
		public OperationResult() { }

		/// <summary>
		/// Create fail OperationResult with message
		/// </summary>
		/// <param name="message"></param>
		public OperationResult(string message)
		{
			Success = false;
			Message = message;
		}

		public static implicit operator OperationResult(bool b)
		{
			return new OperationResult { Success = b };
		}
	}
}