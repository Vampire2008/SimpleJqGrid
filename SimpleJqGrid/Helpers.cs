using System.Web.Mvc;

namespace SimpleJqGrid
{
	public static class Helpers
	{
		public static ModelStateDictionary BindModel<T>(ref T model, ControllerContext controllerContext)
		{
			var innerModel = model;
			var binder = ModelBinders.Binders.GetBinder(typeof(T));
			var modelState = new ModelStateDictionary();

			var bindingContext = new ModelBindingContext
			{
				ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => innerModel, typeof(T)),
				ModelState = modelState,
				ValueProvider = controllerContext.Controller.ValueProvider
			};

			model = (T)binder.BindModel(controllerContext, bindingContext);

			return modelState;
		}
	}
}