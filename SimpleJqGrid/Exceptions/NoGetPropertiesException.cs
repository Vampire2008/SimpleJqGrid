using System;

namespace SimpleJqGrid.Exceptions
{
	public class NoGetPropertiesException : Exception
	{
		public NoGetPropertiesException()
			: base("There anen't get properties in model")
		{


		}
	}
}