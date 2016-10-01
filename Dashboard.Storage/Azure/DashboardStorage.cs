using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace Dashboard.Azure
{
    // TODO: delete? 
    public sealed class DashboardStorage
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly CloudTable _testCacheCounterTable;
        private readonly CloudBlobContainer _testResultsContainer;

        public CloudStorageAccount StorageAccount => _storageAccount;
        public CloudTable TestCacheCounterTable => _testCacheCounterTable;
        public CloudBlobContainer TestResultsContainer => _testResultsContainer;

        public DashboardStorage(string connectionString) : this(CloudStorageAccount.Parse(connectionString))
        {

        }

        public DashboardStorage(CloudStorageAccount storageAccount)
        {
            _storageAccount = storageAccount;

            var tableClient = _storageAccount.CreateCloudTableClient();
            _testCacheCounterTable = tableClient.GetTableReference(AzureConstants.TableNames.CounterTestCache);

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
    }
}
