using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Mstc.Core.Providers;
using Newtonsoft.Json.Serialization;
using Umbraco.Core;

namespace MSTC.Web.EventHandlers
{
    public class ApplicationEventHandler : IApplicationEventHandler
    {
        public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            RegisterCustomRoutes();

            var contentEventHandler = new ContentEventHandler();
            Umbraco.Core.Services.ContentService.Saving += contentEventHandler.ContentService_Saving;
            Umbraco.Core.Services.ContentService.Saved += contentEventHandler.ContentService_Saved;
            Umbraco.Core.Services.ContentService.Deleting += contentEventHandler.ContentService_Deleting;
        }

        private static void RegisterCustomRoutes()
        {            
            RouteTable.Routes.MapRoute(name: "PaymentController",
                                       url: "Payment/{action}/{id}",
                                       defaults: new { controller = "Payment", action = "Index", id = UrlParameter.Optional });
            RouteTable.Routes.MapRoute(name: "MandateController",
                                       url: "Mandate/{action}/{id}",
                                       defaults: new { controller = "Mandate", action = "Index", id = UrlParameter.Optional });
        }

        public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            
        }

        public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            HttpConfiguration config = GlobalConfiguration.Configuration;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.UseDataContractJsonSerializer = false;
        }     
    }
}