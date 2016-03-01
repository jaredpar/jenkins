using Dashboard.Helpers;
using Dashboard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Dashboard.Controllers
{
    public class StatusController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction(nameof(Tests));
        }

        public ActionResult Tests()
        {
            var stats = TestCacheStats.Instance;
            return View(stats.GetCurrentSummary());
        }
    }
}