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
    public class JobsController: Controller
    {
        public ActionResult Index()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Jenkins"].ConnectionString;
            using (var client = new DataClient(connectionString))
            {
                var model = new JobModel();
                model.Names.AddRange(client.GetJobNames());
                return View(model);
            }
        }
    }
}