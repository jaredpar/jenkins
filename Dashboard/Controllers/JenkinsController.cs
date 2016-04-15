using Dashboard.Helpers;
using Dashboard.Models;
using Roslyn.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public ActionResult Jobs(string id = null, string view = null)
        {
            return string.IsNullOrEmpty(id)
                ? GetJobList(view)
                : GetJob(id);
        }

        public ActionResult Views()
        {
            var viewList = CreateJenkinsClient().GetViews();
            return View(viewName: "ViewList", model: new ViewModel(viewList));
        }

        public ActionResult Queue(string id = null)
        {
            // FOLDER: Need a string representation we can serialize back and forth to a JobId and use it here
            return string.IsNullOrEmpty(id)
                ? GetQueueJobList()
                : GetQueueJob(new JobId(shortName: id), Request.GetParamInt("count", DefaultQueueJobCount));
        }

        public ActionResult Waiting()
        {
            var minimumCount = Request.GetParamInt("minimum", defaultValue: 3);
            var groups = CreateRoslynClient().Client
                .GetQueuedItemInfoList()
                .Where(x => x.PullRequestInfo != null)
                .GroupBy(x => x.JobName)
                .Where(x => x.Count() >= minimumCount);

            var model = new WaitingModel()
            {
                MinimumCount = minimumCount,
                Items = groups
            };

            return View(viewName: "Waiting", model: model);
        }

        private ActionResult GetJobList(string view)
        {
            var model = new AllJobsModel();
            var client = CreateJenkinsClient();
            var list = string.IsNullOrEmpty(view)
                ? client.GetJobIds()
                : client.GetJobIdsInView(view);
            foreach (var id in list)
            {
                model.Names.Add(id.Name);
            }

            return View(viewName: "JobList", model: model);
        }

        private ActionResult GetJob(string jobName)
        {
            // FOLDER: Need to convert to a JobId
            var model = GetJobDaySummary(new JobId(jobName));
            return View(viewName: "JobData", model: model);
        }

        private JobSummary GetJobDaySummary(JobId jobId)
        {
            var client = CreateRoslynClient().Client;
            var all = client.GetBuildInfoList(jobId).Where(x => x.State != BuildState.Running);
            var list = new List<JobDaySummary>();
            foreach (var group in all.GroupBy(x => x.Date.Date))
            {
                var succeeded = group.Count(x => x.State == BuildState.Succeeded);
                var failed = group.Count(x => x.State == BuildState.Failed);
                var aborted = group.Count(x => x.State == BuildState.Aborted);
                var averageDuration = TimeSpan.FromMilliseconds(group.Average(x => x.Duration.TotalMilliseconds));
                list.Add(new JobDaySummary()
                {
                    Name = jobId.Name,
                    Date = group.Key,
                    Succeeded = succeeded,
                    Failed = failed,
                    Aborted = aborted,
                    AverageDuration = averageDuration
                });
            }

            return new JobSummary()
            {
                Name = jobId.Name,
                AverageDuration = TimeSpan.FromMilliseconds(all.Average(x => x.Duration.TotalMilliseconds)),
                JobDaySummaryList = list
            };
        }

        private ActionResult GetQueueJobList()
        {
            var client = CreateRoslynClient();
            var list = client.GetJobNames();
            return View(viewName: "QueueJobList", model: list);
        }

        private ActionResult GetQueueJob(JobId jobId, int count)
        {
            var list = GetJobSummaryList(jobId, count);

            // TODO: this is expensive to compute.  Should cache.
            var average = TimeSpan.FromMilliseconds(list.Average(x => x.QueueTime.TotalMilliseconds));
            var median = TimeSpan.FromMilliseconds(list.Select(x => x.QueueTime.TotalMilliseconds).OrderBy(x => x).Skip(list.Count / 2).First());
            var max = TimeSpan.FromMilliseconds(list.Max(x => x.QueueTime.TotalMilliseconds));
            var min = TimeSpan.FromMilliseconds(list.Min(x => x.QueueTime.TotalMilliseconds));
            var model = new JobQueueModel()
            {
                // FOLDER: serializable name here? 
                JobName = jobId.Name,
                JobCount = list.Count,
                AverageTime = average,
                MedianTime = median,
                MaxTime = max,
                Mintime = min,
                Jobs = list
            };

            return View(viewName: "QueueJobData", model: model);
        }

        private List<JobQueueSummary> GetJobSummaryList(JobId jobId, int count)
        {
            var list = new List<JobQueueSummary>();
            var roslynClient = CreateRoslynClient();
            var client = roslynClient.Client;

            foreach (var id in client.GetBuildIds(jobId).Take(count))
            {
                var state = client.GetBuildInfo(id).State;
                if (state == BuildState.Running)
                {
                    continue;
                }

                var time = roslynClient.GetTimeInQueue(id);
                if (time.HasValue)
                {
                    var summary = new JobQueueSummary()
                    {
                        // FOLDER: Serializable name here? 
                        Name = jobId.Name,
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