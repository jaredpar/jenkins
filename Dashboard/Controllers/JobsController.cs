﻿using Dashboard.Models;
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
                var model = new AllJobsModel();
                model.Names.AddRange(client.GetJobNames());
                return View(model);
            }
        }

        public ActionResult Name(string name = null)
        {
            name = name ?? "roslyn_master_win_dbg_unit32";

            var connectionString = ConfigurationManager.ConnectionStrings["Jenkins"].ConnectionString;
            using (var client = new DataClient(connectionString))
            {
                var duration = client.GetAverageDuration(name);
                var model = new JobModel()
                {
                    Name = name,
                    AverageDuration = duration
                };
                model.DailyAverageDuration.AddRange(client.GetDailyAverageDurations(name));

                return View(model);
            }
        }
    }
}