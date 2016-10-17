using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    /// <summary>
    /// This type is used to implement counter functionality on the granularity of a day.
    /// 
    /// The approach is to let the date be the partition key.  The row key is a GUID.  This 
    /// allows all writers to be unique.  Each writer stores a separate copy of the counter
    /// that can be aggregated at query time.
    ///
    /// It is safe to use this type for getting / writing <see cref="ITableEntity"/> values from
    /// multiple threads.  Each thread will get a different GUID to avoid write contention.
    ///
    /// The provided <see cref="CloudTable"/> is restricted to only hold an entity of a single
    /// type.
    /// </summary>
    public sealed class CounterUtil<T>
        where T : class, ITableEntity, new()
    {
        /// <summary>
        /// The stack of T entities which can be updated.  Values are considered owned by the 
        /// thread which pops them.  Ownership is released by pushing back onto the stack.
        /// </summary>
        private ConcurrentStack<T> _stack;

        public CloudTable Table { get; }
        public int ApproximateCacheCount => _stack.Count;

        public CounterUtil(CloudTable table) : this(table, new ConcurrentStack<T>())
        {

        }

        internal CounterUtil(CloudTable table, ConcurrentStack<T> stack)
        {
            Table = table;
            _stack = stack;
        }

        /// <summary>
        /// Get the <see cref="ITableEntity"/> to be updated.
        /// </summary>
        /// <remarks>
        /// This function must be called with a lock held
        /// </remarks>
        private T AcquireEntity()
        {
            var partitionKey = GetCurrentParitionKey();

            T entity;
            do
            {
                if (!_stack.TryPop(out entity))
                {
                    break;
                }
            } while (entity.PartitionKey != partitionKey.Key);

            if (entity == null)
            {
                entity = new T();
                entity.PartitionKey = partitionKey.Key;
                entity.RowKey = Guid.NewGuid().ToString("N");
            }

            return entity;
        }

        public void Update(Action<T> action)
        {
            var entity = AcquireEntity();
            action(entity);
            var operation = TableOperation.InsertOrReplace(entity);
            Table.Execute(operation);
            _stack.Push(entity);
        }

        public async Task UpdateAsync(Action<T> action)
        {
            var entity = AcquireEntity();
            action(entity);
            var operation = TableOperation.InsertOrReplace(entity);
            await Table.ExecuteAsync(operation);
            _stack.Push(entity);
        }

        public IEnumerable<T> Query(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = GetQueryString(startDate, endDate);
            return AzureUtil.Query<T>(Table, query);
        }

        public IEnumerable<T> Query(DateTimeOffset date, CancellationToken cancellationToken = default(CancellationToken))
        {
            var key = DateTimeKey.GetDateKey(date);
            return AzureUtil.Query<T>(Table, TableQueryUtil.PartitionKey(key));
        }

        public async Task<List<T>> QueryAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = GetQueryString(startDate, endDate);
            return await AzureUtil.QueryAsync<T>(Table, query, cancellationToken);
        }

        public async Task<List<T>> QueryAsync(DateTimeOffset date, CancellationToken cancellationToken = default(CancellationToken))
        {
            var key = DateTimeKey.GetDateKey(date);
            var query = TableQueryUtil.PartitionKey(key);
            return await AzureUtil.QueryAsync<T>(Table, query, cancellationToken);
        }

        public static DateTimeKey GetCurrentParitionKey() => new DateTimeKey(DateTimeOffset.UtcNow, DateTimeKeyFlags.Date);

        private static string GetQueryString(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            var startKey = DateTimeKey.GetDateKey(startDate);
            var endKey = DateTimeKey.GetDateKey(endDate);
            if (startKey == endKey)
            {
                return TableQueryUtil.PartitionKey(startKey);
            }

            return TableQueryUtil.And(
                TableQueryUtil.PartitionKey(startKey, ColumnOperator.GreaterThanOrEqual),
                TableQueryUtil.PartitionKey(endKey, ColumnOperator.LessThanOrEqual));
        }
    }
}
