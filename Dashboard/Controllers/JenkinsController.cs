using Dashboard.Models;
using Roslyn.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Dashboard.Controllers
{
    public class JenkinsController : Controller
    {
        public ActionResult Index()
        {
            return View(GetAllJobs());
        }

        public ActionResult Job(string name)
        {
            if (name == null)
            {
                return View(viewName: "Index", model: GetAllJobs());
            }
            return View();
        }

        private static AllJobsModel GetAllJobs()
        {
            var model = new AllJobsModel();
            var client = CreateClient();
            foreach (var name in client.GetJobNames())
            {
                model.Names.Add(name);
            }
            return model;
        }

        private static RoslynClient CreateClient()
        {
            return new RoslynClient();
        }
    }
}