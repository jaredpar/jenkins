using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public static class AzureUtil
    {
        // Current Azure Limit
        internal const int MaxBatchCount = 100;

        public const string ViewNameAll = "all";
        public const string ViewNameRoot = "root";
        public const string ViewNameRoslyn = "dotnet_roslyn";

        public static readonly DateTimeOffset DefaultStartDate = new DateTimeOffset(year: 2016, month: 3, day: 1, hour: 0, minute: 0, second: 0, offset: TimeSpan.Zero);

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
            where T : ITableEntity
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
            where T : ITableEntity
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


        /// <summary>
        /// Delete a raw list that is not grouped by partition keys. 
        /// </summary>
        public static async Task DeleteBatchUnordered<T>(CloudTable table, IEnumerable<T> entityList)
            where T : ITableEntity
        {
            foreach (var group in entityList.GroupBy(x => x.PartitionKey))
            {
                await DeleteBatch(table, group.ToList());
            }
        }

        /// <summary>
        /// Delete a collection of <see cref="TableEntity"/> values into a table using batch style 
        /// operations.  All entities must be insertable via batch operations.
        /// </summary>
        public static async Task DeleteBatch<T>(CloudTable table, List<T> entityList)
            where T : ITableEntity
        {
            if (entityList.Count == 0)
            {
                return;
            }

            var operation = new TableBatchOperation();
            foreach (var entity in entityList)
            {
                operation.Delete(entity);

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

        // TODO: Delete
        public static IEnumerable<T> Query<T>(CloudTable table, string filter)
            where T : ITableEntity, new()
        {
            var query = new TableQuery<T>().Where(filter);
            return table.ExecuteQuery(query);
        }

        /// <summary>
        /// Delete an entity with the specified partition and row key.  This delete is unconditional and 
        /// doesn't do any conflict checking.
        /// </summary>
        public static Task DeleteAsync(
            CloudTable table,
            EntityKey key,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var entity = new DynamicTableEntity();
            entity.PartitionKey = key.PartitionKey;
            entity.RowKey = key.RowKey;
            entity.ETag = "*";
            var operation = TableOperation.Delete(entity);
            return table.ExecuteAsync(operation);
        }

        /// <summary>
        /// Delete an entity with the specified partition and row key.  This delete is unconditional and 
        /// doesn't do any conflict checking.  This method doesn't throw any errors on a failed delete.
        /// </summary>
        public static async Task MaybeDeleteAsync(
            CloudTable table,
            EntityKey key,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await DeleteAsync(table, key, cancellationToken);
            }
            catch
            {
                // Errors okay here.  Method specifically swallowing when the entity doesn't already exist.
            }
        }

        /// <summary>
        /// Query async for a single entity value matching the specified key
        /// </summary>
        public static async Task<T> QueryAsync<T>(
            CloudTable table,
            EntityKey key,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : ITableEntity, new()
        {
            var query = new TableQuery<T>().Where(TableQueryUtil.Key(key));
            var segment = await table.ExecuteQuerySegmentedAsync(query, null, cancellationToken);
            if (segment.Results.Count == 0)
            {
                return default(T);
            }

            return segment.Results[0];
        }

        public static async Task QueryAsync<T>(
            CloudTable table,
            TableQuery<T> query,
            Func<T, Task> callback,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : ITableEntity, new()
        {
            TableContinuationToken token = null;
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync(query, token, cancellationToken);
                token = segment.ContinuationToken;

                var results = segment.Results;
                for (var i = 0; i < results.Count; i++)
                {
                    var entity = results[i];
                    await callback(entity);
                }

            } while (token != null);
        }

        public static async Task<List<T>> QueryAsync<T>(
            CloudTable table,
            string query,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : ITableEntity, new()
        {
            var tableQuery = new TableQuery<T>().Where(query);
            return await QueryAsync(table, tableQuery, cancellationToken);
        }

        public static async Task<List<T>> QueryAsync<T>(
            CloudTable table,
            TableQuery<T> query,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : ITableEntity, new()
        {
            var list = new List<T>();
            TableContinuationToken token = null;
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync(query, token, cancellationToken);
                token = segment.ContinuationToken;
                list.AddRange(segment.Results);
            } while (token != null);

            return list;
        }

        public static string GetViewName(JobId jobId)
        {
            if (jobId.IsRoot || jobId.Parent.IsRoot)
            {
                return ViewNameRoot;
            }

            var current = jobId;
            while (!current.Parent.IsRoot)
            {
                // TODO: Hack, formalize this process.
                if (current.ShortName == "dotnet_roslyn-internal")
                {
                    return ViewNameRoslyn;
                }

                // Give private jobs the view name of the folder that is directly above
                // Private.
                if (current.Parent.ShortName == "Private")
                {
                    return current.ShortName;
                }

                current = current.Parent;
            }

            return current.ShortName;
        }
    }
}
