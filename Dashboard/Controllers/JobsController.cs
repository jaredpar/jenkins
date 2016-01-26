﻿using Dashboard.Helpers;
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
    public class JobsController : DashboardController
    {
        public ActionResult Index()
        {
            using (var client = CreateDataClient())
            {
                var model = new AllJobsModel();
                model.Names.AddRange(client.GetJobNamesWeighted());
                return View(model);
            }
        }

        public ActionResult Name(string name = null)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<JobDaySummary> GetDailySummary(DataClient client, string jobName)
        {
            var map = new Dictionary<DateTime, JobDaySummary>();
            Action<DateTime, Action<JobDaySummary>> update = (date, callback) =>
            {
                date = date.Date;
                JobDaySummary summary;
                if (!map.TryGetValue(date, out summary))
                {
                    summary = new JobDaySummary();
                    summary.Date = date;
                    map[date] = summary;
                }

                callback(summary);
            };

            client.GetDailyJobCount(jobName, BuildState.Succeeded).ForEach(x => update(x.Item1, y => y.Succeeded = x.Item2));
            client.GetDailyJobCount(jobName, BuildState.Failed).ForEach(x => update(x.Item1, y => y.Failed = x.Item2));
            client.GetDailyJobCount(jobName, BuildState.Aborted).ForEach(x => update(x.Item1, y => y.Aborted = x.Item2));
            return map
                .OrderBy(pair => pair.Key)
                .Select(pair => pair.Value);
        }
    }
}