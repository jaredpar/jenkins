using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public static class AzureUtil
    {
        // Current Azure Limit
        internal const int MaxBatchCount = 100;

        public static readonly DateTime DefaultStartDate = new DateTime(year: 2016, month: 3, day: 1);

        /// <summary>
        /// Ensure all of our Azure resources exist.
        /// </summary>
        /// <param name="storageAccount"></param>
        public static void EnsureAzureResources(CloudStorageAccount storageAccount)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            foreach (var name in AzureConstants.TableNames.All())
            {
                var table = tableClient.GetTableReference(name);
                table.CreateIfNotExists();
            }

            var blobClient = storageAccount.CreateCloudBlobClient();
            foreach (var name in AzureConstants.ContainerNames.All())
            {
                var container = blobClient.GetContainerReference(name);
                container.CreateIfNotExists();
            }

            var queueClient = storageAccount.CreateCloudQueueClient();
            foreach (var name in AzureConstants.QueueNames.All())
            {
                var queue = queueClient.GetQueueReference(name);
                queue.CreateIfNotExists();
            }
        }

        /// <summary>
        /// There are a number of characters which are illegal for partition / row keys in Azure.  This 
        /// method will normalize them to the specified value.
        ///
        /// https://msdn.microsoft.com/en-us/library/dd179338
        /// </summary>
        public static string NormalizeKey(string value, char replace)
        {
            Debug.Assert(!IsIllegalKeyChar(replace));
            if (!value.Any(c => IsIllegalKeyChar(c)))
            {
                return value;
            }

            var builder = new StringBuilder(capacity: value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var c = IsIllegalKeyChar(value[i]) ? replace : value[i];
                builder.Append(c);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Is this one of the characters which is illegal as a partition / row key.  Full list available
        /// here:
        ///
        /// https://msdn.microsoft.com/en-us/library/dd179338
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsIllegalKeyChar(char c)
        {
            switch (c)
            {
                case '/':
                case '\\':
                case '#':
                case '?':
                    return true;
                default:
                    return char.IsControl(c);
            }
        }

        public static bool IsIllegalKey(string key)
        {
            return key.Any(c => IsIllegalKeyChar(c));
        }

        /// <summary>
        /// Insert a raw list that is not grouped by partition keys. 
        /// </summary>
        public static async Task InsertBatchUnordered<T>(CloudTable table, IEnumerable<T> entityList)
            where T : TableEntity
        {
            foreach (var group in entityList.GroupBy(x => x.PartitionKey))
            {
                await InsertBatch(table, group.ToList());
            }
        }

        /// <summary>
        /// Inserts a collection of <see cref="TableEntity"/> values into a table using batch style 
        /// operations.  All entities must be insertable via batch operations.
        /// </summary>
        public static async Task InsertBatch<T>(CloudTable table, List<T> entityList)
            where T : TableEntity
        {
            if (entityList.Count == 0)
            {
                return;
            }

            var operation = new TableBatchOperation();
            foreach (var entity in entityList)
            {
                // Important to use InsertOrReplace here.  It's possible for a populate job to be cut off in the 
                // middle when the BuildFailure table is updated but not yet the BuildProcessed table.  Hence 
                // we'll up here again doing a batch insert.
                operation.InsertOrReplace(entity);

                if (operation.Count == MaxBatchCount)
                {
                    await table.ExecuteBatchAsync(operation);
                    operation.Clear();
                }
            }

            if (operation.Count > 0)
            {
                await table.ExecuteBatchAsync(operation);
            }
        }

    }
}
