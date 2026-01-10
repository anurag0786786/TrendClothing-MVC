using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;

namespace TrendClothing.Utility
{
    public interface IEmailTemplateRenderer
    {
        Task<string> RenderToStringAsync(
            ControllerContext controllerContext,
            string viewPath,
            object model);
    }

    public class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;

        public EmailTemplateRenderer(
            ICompositeViewEngine viewEngine,
            ITempDataProvider tempDataProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
        }

        public async Task<string> RenderToStringAsync(
            ControllerContext controllerContext,
            string viewPath,
            object model)
        {
            var actionContext = new ActionContext(
                controllerContext.HttpContext,
                controllerContext.RouteData,
                controllerContext.ActionDescriptor
            );

            using var sw = new StringWriter();

            var viewResult = _viewEngine.GetView(null, viewPath, false);
            if (!viewResult.Success)
                throw new FileNotFoundException($"View not found: {viewPath}");

            var viewDictionary = new ViewDataDictionary(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary())
            {
                Model = model
            };

            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewDictionary,
                new TempDataDictionary(
                    controllerContext.HttpContext,
                    _tempDataProvider),
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}
