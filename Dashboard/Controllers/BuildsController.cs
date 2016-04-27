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
            var summary = GetBuildFailureSummary(pr, startDate, limit);
            return View(viewName: "TestFailureList", model: summary);
        }

        /// <summary>
        /// Summarize the details of an individual failure.
        /// </summary>
        public ActionResult TestFailure(string name = null, bool pr = true, DateTime? startDate = null)
        {
            var startDateValue = _storage.GetStartDateValue(startDate);
            var model = new TestFailureModel()
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

            return View(viewName: "TestFailure", model: model);
        }

        public ActionResult Result(string name = null, bool pr = false, DateTime? startDate = null, int limit = 10)
        {
            var startDateValue = _storage.GetStartDateValue(startDate);
            var table = _storage.StorageAccount.CreateCloudTableClient().GetTableReference(BuildResultEntity.TableName);
            var key = new DateKey(startDateValue);

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
            var summary = GetBuildFailureSummary(pr, startDateValue, limit);
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

            var table = _storage.StorageAccount.CreateCloudTableClient().GetTableReference(BuildResultEntity.TableName);
            var query = new TableQuery<BuildResultEntity>()
                .Where(AzureUtil.GenerateFilterConditionSinceDate(nameof(BuildResultEntity.PartitionKey), startDate));

            var queryResult = table
                .ExecuteQuery(query)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId.Name))
                .Where(x => x.BuildResultKind != BuildResultKind.Succeeded)
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

        private BuildResultModel GetBuildResultModel(string name, bool pr, DateTimeOffset startDate)
        {
            var model = new BuildResultModel()
            {
                IncludePullRequests = pr,
                StartDate = startDate,
                JobId = JobId.ParseName(name),
            };

            var filter = TableQuery.CombineFilters(
                AzureUtil.GenerateFilterConditionSinceDate(nameof(BuildResultEntity.PartitionKey), startDate),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(BuildResultEntity.JobName), QueryComparisons.Equal, name));
            var table = _storage.StorageAccount.CreateCloudTableClient().GetTableReference(BuildResultEntity.TableName);
            var query = new TableQuery<BuildResultEntity>().Where(filter);

            var queryResult = table
                .ExecuteQuery(query)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId.Name))
                .Where(x => x.BuildResultKind != BuildResultKind.Succeeded)
                .OrderBy(x => x.BuildNumber);
            
            model.Entries.AddRange(queryResult);
            return model;
        }
    
        public TestFailureSummary GetBuildFailureSummary(bool pr = false, DateTime? startDate = null, int limit = 10)
        {
            var startDateValue = _storage.GetStartDateValue(startDate);
            var failureQuery = _storage.GetBuildFailureEntities(startDateValue)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.BuildId.JobName))
                .GroupBy(x => x.RowKey)
                .Select(x => new { Key = x.Key, Count = x.Count() })
                .OrderByDescending(x => x.Count)
                .Take(limit);

            var summary = new TestFailureSummary()
            {
                IncludePullRequests = pr,
                StartDate = startDateValue,
                Limit = limit,
            };

            foreach (var pair in failureQuery)
            {
                var entry = new TestFailureEntry()
                {
                    Name = pair.Key,
                    Count = pair.Count
                };
                summary.Entries.Add(entry);
            }

            return summary;
        }
    }
}