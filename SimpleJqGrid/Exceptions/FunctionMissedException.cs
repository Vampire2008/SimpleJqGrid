using System;

namespace SimpleJqGrid.Exceptions
{
	public class FunctionMissedException : Exception
	{
		public FunctionMissedException(string message)
			: base(message)
		{

		}
	}
}