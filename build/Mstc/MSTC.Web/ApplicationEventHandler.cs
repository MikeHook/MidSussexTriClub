using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Umbraco.Core;

namespace MSTC.Web
{
    public class ApplicationEventHandler : IApplicationEventHandler
    {
        public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            RegisterCustomRoutes();
        }

        private static void RegisterCustomRoutes()
        {
            // this is just an example, modify to suit your controllers and actions
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
            
        }
    }
}