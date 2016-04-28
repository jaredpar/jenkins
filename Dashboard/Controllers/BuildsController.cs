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
using System.Text;

namespace Dashboard.Controllers
{
    public class BuildsController : Controller
    {
        private readonly DashboardStorage _storage;
        private readonly BuildUtil _buildUtil;

        public BuildsController()
        {
            var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            _storage = new DashboardStorage(connectionString);
            _buildUtil = new BuildUtil(_storage.StorageAccount);
        }

        /// <summary>
        /// Lists all of the build failures.
        /// </summary>
        public ActionResult Index(bool pr = false, DateTimeOffset? startDate = null, int limit = 10)
        {
            return Test(name: null, pr: pr, startDate: startDate, limit: limit);
        }

        /// <summary>
        /// Summarize the details of an individual failure.
        /// </summary>
        public ActionResult Test(string name = null, bool pr = false, DateTimeOffset? startDate = null, int limit = 10)
        {
            var startDateValue = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            if (name == null)
            {
                var model = GetTestFailureSummaryModel(pr, startDateValue, limit);
                return View(viewName: "TestFailureList", model: model);
            }
            else
            {
                var model = GetTestFailureModel(name, pr, startDateValue);
                return View(viewName: "TestFailure", model: model);
            }
        }

        public ActionResult Result(string name = null, bool pr = false, DateTime? startDate = null, int limit = 10)
        {
            var startDateValue = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            if (name == null)
            {
                var model = GetBuildResultSummaryModel(pr, startDateValue, limit);
                return View(viewName: "BuildResultList", model: model);
            }
            else
            {
                var model = GetBuildResultModel(name, pr, startDateValue);
                return View(viewName: "BuildResult", model: model);
            }
        }

        public string Csv(bool pr = false, DateTime? startDate = null, int limit = 10)
        {
            var startDateValue = startDate ?? DateTime.UtcNow - TimeSpan.FromDays(7);
            var summary = GetTestFailureSummaryModel(pr, startDateValue, limit);
            var builder = new StringBuilder();
            foreach (var entry in summary.Entries)
            {
                var name = entry.Name.Replace(',', ' ');
                var index = name.LastIndexOf('.');
                var suiteName = name.Substring(0, index);
                var testName = name.Substring(index + 1);
                builder.AppendLine($"{suiteName},{testName},{entry.Count}");
            }

            return builder.ToString();
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

        private BuildResultSummaryModel GetBuildResultSummaryModel(bool pr, DateTimeOffset startDate, int limit)
        {
            var model = new BuildResultSummaryModel()
            {
                IncludePullRequests = pr,
                StartDate = startDate,
                Limit = limit
            };

            var queryResult = _buildUtil
                .GetBuildResults(startDate)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId.Name))
                .Where(x => x.ClassificationKind != ClassificationKind.Succeeded)
                .GroupBy(x => x.JobId)
                .Select(x => new { JobId = x.Key, Count = x.Count() })
                .OrderByDescending(x => x.Count)
                .Take(limit);
            foreach (var entity in queryResult)
            {
                var entry = new BuildResultSummaryEntry()
                {
                    JobId = entity.JobId,
                    Count = entity.Count
                };

                model.Entries.Add(entry);
            }

            return model;
        }

        private BuildResultModel GetBuildResultModel(string jobName, bool pr, DateTimeOffset startDate)
        {
            var model = new BuildResultModel()
            {
                IncludePullRequests = pr,
                StartDate = startDate,
                JobId = JobId.ParseName(jobName),
            };

            var queryResult = _buildUtil
                .GetBuildResults(startDate, jobName)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId.Name))
                .Where(x => x.ClassificationKind != ClassificationKind.Succeeded)
                .OrderBy(x => x.BuildNumber);
            
            model.Entries.AddRange(queryResult);
            return model;
        }
    
        private TestFailureSummaryModel GetTestFailureSummaryModel(bool pr, DateTimeOffset startDate, int limit)
        {
            var failureQuery = _buildUtil
                .GetTestCaseFailures(startDate)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.BuildId.JobName))
                .GroupBy(x => x.RowKey)
                .Select(x => new { Key = x.Key, Count = x.Count() })
                .OrderByDescending(x => x.Count)
                .Take(limit);

            var summary = new TestFailureSummaryModel()
            {
                IncludePullRequests = pr,
                StartDate = startDate,
                Limit = limit,
            };

            foreach (var pair in failureQuery)
            {
                var entry = new TestFailureSummaryEntry()
                {
                    Name = pair.Key,
                    Count = pair.Count
                };
                summary.Entries.Add(entry);
            }

            return summary;
        }

        private TestFailureModel GetTestFailureModel(string name, bool pr, DateTimeOffset startDate)
        {
            var model = new TestFailureModel()
            {
                Name = name,
                IncludePullRequests = pr,
                StartDate = startDate
            };

            foreach (var entity in _buildUtil.GetTestCaseFailures(startDate, name))
            {
                var buildId = entity.BuildId;
                if (!pr && JobUtil.IsPullRequestJobName(buildId.JobName))
                {
                    continue;
                }

                model.Builds.Add(entity);
            }

            return model;
        }
    }
}