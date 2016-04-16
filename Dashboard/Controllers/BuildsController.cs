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

namespace Dashboard.Controllers
{
    public class BuildsController : Controller
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly CloudTable _buildFailureTable;
        private readonly CloudTable _buildProcessedTable;

        public BuildsController()
        {
            var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            _storageAccount = CloudStorageAccount.Parse(connectionString);

            var tableClient = _storageAccount.CreateCloudTableClient();
            _buildFailureTable = tableClient.GetTableReference(AzureConstants.TableNameBuildFailure);
            _buildProcessedTable = tableClient.GetTableReference(AzureConstants.TableNameBuildProcessed);
        }

        /// <summary>
        /// Lists all of the build failures.
        /// </summary>
        public ActionResult Index(bool pr = false, DateTime? startDate = null, int limit = 10)
        {
            var startDateValue = GetStartDateValue(startDate);
            var query = new TableQuery<BuildFailureEntity>().Where(GenerateFilterBuildFailureDate(startDateValue));

            var failureQuery = _buildFailureTable.ExecuteQuery(query)
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
            var startDateValue = GetStartDateValue(startDate);
            var dateFilter = GenerateFilterBuildFailureDate(startDateValue);
            var rowFilter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.RowKey),
                QueryComparisons.Equal,
                name);
            var query = new TableQuery<BuildFailureEntity>().Where(TableQuery.CombineFilters(rowFilter, TableOperators.And, dateFilter));
            var model = new BuildFailureModel()
            {
                Name = name,
                IncludePullRequests = pr,
                StartDate = startDateValue
            };

            foreach (var entity in _buildFailureTable.ExecuteQuery(query))
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

        private static DateTime GetStartDateValue(DateTime? startDate)
        {
            return startDate?.ToUniversalTime().Date ?? DateTime.UtcNow.Date - TimeSpan.FromDays(1);
        }

        private static string GenerateFilterBuildFailureDate(DateTime startDate)
        {
            Debug.Assert(startDate.Kind == DateTimeKind.Utc);
            return TableQuery.GenerateFilterConditionForDate(nameof(BuildFailureEntity.BuildDate), QueryComparisons.GreaterThanOrEqual, new DateTimeOffset(startDate));
        }
    }
}