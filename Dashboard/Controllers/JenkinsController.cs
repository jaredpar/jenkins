﻿using Dashboard.Helpers;
using Dashboard.Models;
using Roslyn.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Dashboard.Controllers
{
    public class JenkinsController : DashboardController
    {
        public static int DefaultQueueJobCount = 100;

        public ActionResult Index()
        {
            return Jobs();
        }

        public ActionResult Jobs(string id = null)
        {
            return string.IsNullOrEmpty(id)
                ? GetJobList()
                : GetJob(id);
        }

        public ActionResult Queue(string id = null)
        {
            return string.IsNullOrEmpty(id)
                ? GetQueueJobList()
                : GetQueueJob(id, Request.GetParamInt("count", DefaultQueueJobCount));
        }

        private ActionResult GetJobList()
        {
            var model = new AllJobsModel();
            var client = CreateRoslynClient();
            foreach (var name in client.GetJobNames())
            {
                model.Names.Add(name);
            }

            return View(viewName: "JobList", model: model);
        }

        private ActionResult GetJob(string jobName)
        {
            throw new NotImplementedException();
        }

        private ActionResult GetQueueJobList()
        {
            var client = CreateRoslynClient();
            var list = client.GetJobNames();
            return View(viewName: "QueueJobList", model: list);
        }

        private ActionResult GetQueueJob(string jobName, int count)
        {
            var list = GetJobSummaryList(jobName, count);

            // TODO: this is expensive to compute.  Should cache.
            var average = TimeSpan.FromMilliseconds(list.Average(x => x.QueueTime.TotalMilliseconds));
            var median = TimeSpan.FromMilliseconds(list.Select(x => x.QueueTime.TotalMilliseconds).OrderBy(x => x).Skip(list.Count / 2).First());
            var max = TimeSpan.FromMilliseconds(list.Max(x => x.QueueTime.TotalMilliseconds));
            var min = TimeSpan.FromMilliseconds(list.Min(x => x.QueueTime.TotalMilliseconds));
            var model = new JobQueueModel()
            {
                JobName = jobName,
                JobCount = list.Count,
                AverageTime = average,
                MedianTime = median,
                MaxTime = max,
                Mintime = min,
                Jobs = list
            };

            return View(viewName: "QueueJobData", model: model);
        }

        private List<JobQueueSummary> GetJobSummaryList(string jobName, int count)
        {
            var list = new List<JobQueueSummary>();
            var roslynClient = CreateRoslynClient();
            var client = roslynClient.Client;

            foreach (var id in client.GetBuildIds(jobName).Take(count))
            {
                var state = client.GetBuildState(id);
                if (state == BuildState.Running)
                {
                    continue;
                }

                var time = roslynClient.GetTimeInQueue(id);
                if (time.HasValue)
                {
                    var summary = new JobQueueSummary()
                    {
                        Name = jobName,
                        Id = id.Id,
                        QueueTime = time.Value
                    };
                    list.Add(summary);
                }
            }

            return list.OrderBy(x => x.Id).ToList();
        }
    }
}