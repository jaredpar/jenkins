using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Dashboard
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Jobs",
                url: "jobs",
                defaults: new { controller = "Jobs", action = "Index" }
            );

            routes.MapRoute(
                name: "JobsIndex",
                url: "jobs/index",
                defaults: new { controller = "Jobs", action = "Index" }
            );

            routes.MapRoute(
                name: "JobsName",
                url: "jobs/{name}",
                defaults: new { controller = "Jobs", action = "Name" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
