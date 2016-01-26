using Dashboard.Helpers;
using Dashboard.Models;
using Roslyn.Jenkins;
using Roslyn.Sql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Dashboard.Controllers
{
    public class QueueController : DashboardController
    {
        public ActionResult Index(string jobName = null, int? count = null)
        {
            return RedirectToAction(controllerName: "Jenkins", actionName: "Queue", routeValues: new { jobName = jobName, count = count });
        }
    }
}