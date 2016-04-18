using Dashboard.Models;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Dashboard;
using Dashboard.Azure;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Dashboard.Helpers;

namespace Dashboard.Controllers
{
    public class BuildsController : Controller
    {
        private readonly DashboardStorage _storage;

        public BuildsController()
        {
            var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            _storage = new DashboardStorage(connectionString);
        }

        /// <summary>
        /// Lists all of the build failures.
        /// </summary>
        public ActionResult Index(bool pr = false, DateTime? startDate = null, int limit = 10)
        {
            var startDateValue = _storage.GetStartDateValue(startDate);
            var failureQuery = _storage.GetBuildFailureEntities(startDateValue)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.BuildId.JobName))
                .GroupBy(x => x.RowKey)
                .Select(x => new { Key = x.Key, Count = x.Count() })
                .OrderByDescending(x => x.Count)
                .Take(limit);

            var summary = new BuildFailureSummary()
            {
                IncludePullRequests = pr,
                StartDate = startDateValue,
                Limit = limit,
            };

            foreach (var pair in failureQuery)
            {
                var entry = new BuildFailureEntry()
                {
                    Name = pair.Key,
                    Count = pair.Count
                };
                summary.Entries.Add(entry);
            }

            return View(viewName: "FailureList", model: summary);
        }

        /// <summary>
        /// Summarize the details of an individual failure.
        /// </summary>
        public ActionResult Failure(string name = null, bool pr = true, DateTime? startDate = null)
        {
            var startDateValue = _storage.GetStartDateValue(startDate);
            var model = new BuildFailureModel()
            {
                Name = name,
                IncludePullRequests = pr,
                StartDate = startDateValue
            };

            foreach (var entity in _storage.GetBuildFailureEntities(name, startDateValue))
            {
                var buildId = entity.BuildId;
                if (!pr && JobUtil.IsPullRequestJobName(buildId.JobName))
                {
                    continue;
                }

                model.Builds.Add(entity);
            }

            return View(viewName: "Failure", model: model);
        }

        public ActionResult Demand(string userName, string commit)
        {
            var runStatus = new DemandRunStatusModel()
            {
                UserName = userName,
                Commit = commit,
            };

            var query = new TableQuery<DemandBuildEntity>()
                .Where(DashboardStorage.GenerateDemandBuildFilter(userName, commit));
            foreach (var entity in _storage.DemandBuildTable.ExecuteQuery(query))
            {
                var status = new DemandBuildStatusModel()
                {
                    BuildNumber = entity.BuildNumber,
                    JobName = entity.JobName,
                    QueueNumber = entity.QueueItemNumber
                };
                runStatus.StatusList.Add(status);
            }

            return View(viewName: "DemandStatus", model: runStatus);
        }
    }
}