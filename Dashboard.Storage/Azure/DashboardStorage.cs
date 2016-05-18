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
        private readonly CloudTable _demandRunTable;
        private readonly CloudTable _demandBuildTable;
        private readonly CloudTable _testCacheCounterTable;
        private readonly CloudBlobContainer _testResultsContainer;

        public CloudStorageAccount StorageAccount => _storageAccount;
        public CloudTable DemandRunTable => _demandRunTable;
        public CloudTable DemandBuildTable => _demandBuildTable;
        public CloudTable TestCacheCounterTable => _testCacheCounterTable;
        public CloudBlobContainer TestResultsContainer => _testResultsContainer;

        public DashboardStorage(string connectionString) : this(CloudStorageAccount.Parse(connectionString))
        {

        }

        public DashboardStorage(CloudStorageAccount storageAccount)
        {
            _storageAccount = storageAccount;

            var tableClient = _storageAccount.CreateCloudTableClient();
            _demandRunTable = tableClient.GetTableReference(AzureConstants.TableNames.DemandRun);
            _demandBuildTable = tableClient.GetTableReference(AzureConstants.TableNames.DemandBuild);
            _testCacheCounterTable = tableClient.GetTableReference(AzureConstants.TableNames.TestCacheCounter);

            var blobClient = _storageAccount.CreateCloudBlobClient();
            _testResultsContainer = blobClient.GetContainerReference(AzureConstants.ContainerNames.TestResults);
        }

        public CloudTable GetTable(string tableName)
        {
            return _storageAccount.CreateCloudTableClient().GetTableReference(tableName);
        }

        public static string NormalizeTestCaseName(string testCaseName)
        {
            return AzureUtil.NormalizeKey(testCaseName, '_');
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
