using NJsonSchema.Generation;
using NSwag.AspNet.Owin;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace WebApi
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            RouteTable.Routes.MapOwinPath("swagger", app =>
            {
                app.UseSwaggerUi(typeof(WebApiApplication).Assembly, settings =>
                {
                    settings.MiddlewareBasePath = "/swagger";
                    settings.GeneratorSettings.DefaultUrlTemplate = "api/{controller}/{action}/{id}";
                    settings.GeneratorSettings.Title = "Release Order API";
                    settings.DocumentTitle = "Release Order API";
                });
            });
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
