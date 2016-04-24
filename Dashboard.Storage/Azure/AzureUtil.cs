using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public static class AzureUtil
    {
        // Current Azure Limit
        internal const int MaxBatchCount = 100;

        /// <summary>
        /// There are a number of characters which are illegal for partition / row keys in Azure.  This 
        /// method will normalize them to the specified value.
        ///
        /// https://msdn.microsoft.com/en-us/library/dd179338
        /// </summary>
        public static string NormalizeKey(string value, char replace)
        {
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

        /// <summary>
        /// Create an exact query for a given entity.
        /// </summary>
        public static TableQuery<T> CreateQuery<T>(EntityKey key)
        {
            var partitionFilter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.PartitionKey),
                QueryComparisons.Equal,
                key.PartitionKey);
            var rowFliter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.RowKey),
                QueryComparisons.Equal,
                key.RowKey);

            return new TableQuery<T>()
                .Where(TableQuery.CombineFilters(
                    partitionFilter,
                    TableOperators.And,
                    rowFliter));
        }

        public static T QueryTable<T>(CloudTable table, EntityKey key)
            where T : ITableEntity, new()
        {
            var query = CreateQuery<T>(key);
            var enumerable = table.ExecuteQuery(query);
            return enumerable.SingleOrDefault();
        }
    }
}
