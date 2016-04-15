using Dashboard.Helpers;
using Dashboard.Models;
using Dashboard.Jenkins;
using Dashboard.Sql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Dashboard.Controllers
{
    /// <summary>
    /// This controller mostly exists to maintain URLs I've already shared with others
    /// </summary>
    public class QueueController : DashboardController
    {
        public ActionResult Index(string jobName = null, int? count = null)
        {
            return RedirectToAction(controllerName: "Jenkins", actionName: "Queue", routeValues: new { jobName = jobName, count = count });
        }
    }
}