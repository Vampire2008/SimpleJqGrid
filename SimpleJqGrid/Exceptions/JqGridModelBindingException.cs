using System;
using System.Web.Mvc;

namespace SimpleJqGrid.Exceptions
{
	public class JqGridModelBindingException : Exception
	{
		public ModelStateDictionary ModelStateDictionary { get; }
		public JqGridModelBindingException(string message, ModelStateDictionary dictionary) : base(message)
		{
			ModelStateDictionary = dictionary;
		}
	}
}