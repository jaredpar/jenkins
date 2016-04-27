using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public sealed class DashboardStorage
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly CloudTable _buildFailureTable;
        private readonly CloudTable _buildProcessedTable;
        private readonly CloudTable _demandRunTable;
        private readonly CloudTable _demandBuildTable;
        private readonly CloudTable _testCacheCounterTable;
        private readonly CloudBlobContainer _testResultsContainer;

        public CloudStorageAccount StorageAccount => _storageAccount;
        public CloudTable DemandRunTable => _demandRunTable;
        public CloudTable DemandBuildTable => _demandBuildTable;
        public CloudTable TestCacheCounterTable => _testCacheCounterTable;
        public CloudBlobContainer TestResultsContainer => _testResultsContainer;

        public DashboardStorage(string connectionString)
        {
            _storageAccount = CloudStorageAccount.Parse(connectionString);

            var tableClient = _storageAccount.CreateCloudTableClient();
            _buildFailureTable = tableClient.GetTableReference(AzureConstants.TableNames.BuildFailure);
            _buildProcessedTable = tableClient.GetTableReference(AzureConstants.TableNames.BuildProcessed);
            _demandRunTable = tableClient.GetTableReference(AzureConstants.TableNames.DemandRun);
            _demandBuildTable = tableClient.GetTableReference(AzureConstants.TableNames.DemandBuild);
            _testCacheCounterTable = tableClient.GetTableReference(AzureConstants.TableNames.TestCacheCounter);

            var blobClient = _storageAccount.CreateCloudBlobClient();
            _testResultsContainer = blobClient.GetContainerReference(AzureConstants.ContainerNames.TestResults);
        }

        public static string NormalizeTestCaseName(string testCaseName)
        {
            return AzureUtil.NormalizeKey(testCaseName, '_');
        }

        public IEnumerable<BuildFailureEntity> GetBuildFailureEntities(DateTime? startDate = null)
        { 
            var startDateValue = GetStartDateValue(startDate);
            var query = new TableQuery<BuildFailureEntity>().Where(GenerateFilterBuildFailureDate(startDateValue));
            return _buildFailureTable.ExecuteQuery(query);
        }

        /// <summary>
        /// Get the <see cref="BuildFailureEntity"/> values for a given job since the specified date.
        /// </summary>
        public IEnumerable<BuildFailureEntity> GetBuildFailureEntities(string name, DateTime? startDate = null)
        {
            name = NormalizeTestCaseName(name);
            var startDateValue = GetStartDateValue(startDate);
            var dateFilter = GenerateFilterBuildFailureDate(startDateValue);
            var rowFilter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.RowKey),
                QueryComparisons.Equal,
                name);
            var query = new TableQuery<BuildFailureEntity>().Where(TableQuery.CombineFilters(rowFilter, TableOperators.And, dateFilter));
            return _buildFailureTable.ExecuteQuery(query);
        }

        public DateTime GetStartDateValue(DateTime? startDate)
        {
            return startDate?.ToUniversalTime().Date ?? DateTime.UtcNow.Date - TimeSpan.FromDays(1);
        }

        public static string GenerateFilterBuildFailureDate(DateTime startDate)
        {
            Debug.Assert(startDate.Kind == DateTimeKind.Utc);
            return TableQuery.GenerateFilterConditionForDate(nameof(BuildFailureEntity.BuildDate), QueryComparisons.GreaterThanOrEqual, new DateTimeOffset(startDate));
        }

        public static string GenerateDemandBuildFilter(string userName, string commit)
        {
            var partitionFilter = TableQuery.GenerateFilterCondition(
                nameof(DemandBuildEntity.PartitionKey),
                QueryComparisons.Equal,
                userName);
            var rowFilter = TableQuery.GenerateFilterCondition(
                nameof(DemandBuildEntity.RowKey),
                QueryComparisons.Equal,
                commit);

            return TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);
        }
    }
}
