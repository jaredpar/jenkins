using Dashboard.Helpers;
using Dashboard.Models;
using Roslyn.Sql;
using System;
using System.Collections.Generic;
using System.Configuration;
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
            // TODO: unify connection string management.
            var connectionString = ConfigurationManager.AppSettings["jenkins-connection-string"];
            using (var stats = new TestCacheStats(connectionString))
            {
                return View(stats.GetCurrentSummary());
            }
        }

        public ActionResult Result(string id)
        {
            // TODO: unify connection string management.
            var connectionString = ConfigurationManager.AppSettings["jenkins-connection-string"];
            var storage = new TestResultStorage(connectionString);

            TestResult testResult;
            if (!storage.TryGetValue(id, out testResult))
            {
                throw new Exception("Invalid key");
            }

            var contentType = testResult.ResultsFileName.EndsWith("xml")
                ? "application/xml"
                : "text/html";
            return Content(testResult.ResultsFileContent, contentType);
        }

        public ActionResult Results()
        {
            // TODO: unify connection string management.
            var connectionString = ConfigurationManager.AppSettings["jenkins-connection-string"];
            var storage = new TestResultStorage(connectionString);
            return View(storage.Keys);
        }

        public ActionResult Errors()
        {
            return View(StorageLogger.Instance.EntryList);
        }
    }
}