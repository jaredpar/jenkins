﻿using Dashboard.Helpers;
using Dashboard.Models;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Dashboard.Controllers
{
    public class JenkinsController : Controller
    {
        public static int DefaultQueueJobCount = 100;

        public ActionResult Index()
        {
            return Jobs();
        }

        public ActionResult Jobs(string name = null, string view = null)
        {
            var client = ControllerUtil.CreateJenkinsClient();
            if (!string.IsNullOrEmpty(name))
            {
                var jobId = JobId.ParseName(name);
                var jobInfo = client.GetJobInfo(jobId);
                if (jobInfo.JobKind == JobKind.Folder)
                {
                    return GetJobList(name, JobListContainerKind.Job, jobInfo.Jobs);
                }

                return GetJob(jobId);
            }

            if (!string.IsNullOrEmpty(view))
            {
                return GetJobList(view, JobListContainerKind.View, client.GetJobIdsInView(view));
            }

            return GetJobList("Root", JobListContainerKind.Root, client.GetJobIds());
        }

        public ActionResult Views()
        {
            var viewList = ControllerUtil.CreateJenkinsClient().GetViews();
            return View(viewName: "ViewList", model: new ViewModel(viewList));
        }

        public ActionResult Queue(string id = null)
        {
            return string.IsNullOrEmpty(id)
                ? GetQueueJobList()
                : GetQueueJob(JobId.ParseName(id), Request.GetParamInt("count", DefaultQueueJobCount));
        }

        public ActionResult Waiting()
        {
            var minimumCount = Request.GetParamInt("minimum", defaultValue: 3);
            var groups = ControllerUtil.CreateJenkinsClient()
                .GetQueuedItemInfoList()
                .Where(x => x.PullRequestInfo != null)
                .GroupBy(x => x.JobId.Name)
                .Where(x => x.Count() >= minimumCount);

            var model = new WaitingModel()
            {
                MinimumCount = minimumCount,
                Items = groups
            };

            return View(viewName: "Waiting", model: model);
        }

        private ActionResult GetJobList(string containerName, JobListContainerKind kind, List<JobId> list)
        {
            var model = new JobListModel()
            {
                ContainerName = containerName,
                Kind = kind,
                NestedList = list
            };

            return View(viewName: "JobList", model: model);
        }

        private ActionResult GetJob(JobId id)
        {
            var model = GetJobDaySummary(id);
            return View(viewName: "JobData", model: model);
        }

        private JobSummary GetJobDaySummary(JobId jobId)
        {
            var client = ControllerUtil.CreateJenkinsClient();
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
            var client = ControllerUtil.CreateJenkinsClient();
            var list = client.GetJobIds().Select(x => x.Name).ToList();
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
            var client = ControllerUtil.CreateJenkinsClient();

            foreach (var id in client.GetBuildIds(jobId).Take(count))
            {
                var state = client.GetBuildInfo(id).State;
                if (state == BuildState.Running)
                {
                    continue;
                }

                var time = client.GetTimeInQueue(id);
                if (time.HasValue)
                {
                    var summary = new JobQueueSummary()
                    {
                        Name = jobId.Name,
                        Id = id.Number,
                        QueueTime = time.Value
                    };
                    list.Add(summary);
                }
            }

            return list.OrderBy(x => x.Id).ToList();
        }
    }
}