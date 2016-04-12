using Dashboard.Models;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Roslyn;
using Roslyn.Azure;
using Roslyn.Jenkins;
using System;
using System.Collections.Generic;
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

        public ActionResult Index(bool pr = true)
        {
            var query = new TableQuery<BuildFailureEntity>().Where(GenerateFilterBuildFailureDate());
            var list = new List<BuildFailureSummary>();

            var failureQuery = _buildFailureTable.ExecuteQuery(query)
                .Where(x => pr || !RoslynClient.IsPullRequestJobName(x.BuildId.JobName))
                .GroupBy(x => x.RowKey)
                .Select(x => new { Key = x.Key, Count = x.Count() })
                .OrderByDescending(x => x.Count);

            foreach (var pair in failureQuery)
            {
                var summary = new BuildFailureSummary()
                {
                    Name = pair.Key,
                    Count = pair.Count
                };
                list.Add(summary);
            }

            return View(viewName: "FailureList", model: list);
        }

        public ActionResult Failure(string name = null, bool pr = true)
        {
            var dateFilter = GenerateFilterBuildFailureDate();
            var rowFilter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.RowKey),
                QueryComparisons.Equal,
                name);
            var query = new TableQuery<BuildFailureEntity>().Where(TableQuery.CombineFilters(rowFilter, TableOperators.And, dateFilter));
            var model = new BuildFailureModel() { Name = name };
            foreach (var entity in _buildFailureTable.ExecuteQuery(query))
            {
                var buildId = entity.BuildId;
                if (!pr && RoslynClient.IsPullRequestJobName(buildId.JobName))
                {
                    continue;
                }

                model.Builds.Add(entity.BuildId);
            }

            return View(viewName: "BuildFailure", model: model);
        }

        private static string GenerateFilterBuildFailureDate(DateTime? startDate = null)
        {
            var startDateValue = startDate?.ToUniversalTime() ?? DateTime.UtcNow - TimeSpan.FromDays(7);
            return TableQuery.GenerateFilterConditionForDate(nameof(BuildFailureEntity.BuildDate), QueryComparisons.GreaterThanOrEqual, startDateValue);
        }
    }
}