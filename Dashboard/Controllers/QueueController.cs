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
        public const int DefaultCount = 100;

        public ActionResult Index(string jobName = null, int? count = null)
        {
            return string.IsNullOrEmpty(jobName)
                ? GetJobList()
                : GetJobQueue(jobName, count ?? DefaultCount);
        }

        private ActionResult GetJobList()
        {
            var client = CreateRoslynClient();
            var list = client.GetJobNames();
            return View(viewName: "JobList", model: list);
        }

        private ActionResult GetJobQueue(string jobName, int count)
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

            return View(viewName: "JobData", model: model);
        }

        private List<JobQueueSummary> GetJobSummaryList(string jobName, int count)
        {
            var list = new List<JobQueueSummary>();
            var roslynClient = CreateRoslynClient();
            var client = roslynClient.Client;

            foreach (var id in client.GetJobIds(jobName).Take(count))
            {
                var state = client.GetJobState(id);
                if (state == JobState.Running)
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