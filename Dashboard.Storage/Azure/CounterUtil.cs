using Microsoft.WindowsAzure.Storage.Table;
using System;
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
        where T : ITableEntity, new()
    {
        private readonly object _guard = new object();

        /// <summary>
        /// Map to allow for efficient lookup of counter entities.  Don't have to query storage every time 
        /// we need to grab it. 
        /// </summary>
        private readonly Dictionary<int, T> _entityMap = new Dictionary<int, T>();

        public CloudTable Table { get; }

        public CounterUtil(CloudTable table)
        {
            Table = table;
        }

        public T GetEntity()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            lock (_guard)
            {
                var partitionKey = GetCurrentParitionKey();
                T entity;
                if (!_entityMap.TryGetValue(id, out entity) || entity.PartitionKey != partitionKey.Key)
                {
                    var rowKey = Guid.NewGuid().ToString("N");
                    entity = new T();
                    entity.PartitionKey = partitionKey.Key;
                    entity.RowKey = Guid.NewGuid().ToString("N");
                    _entityMap[id] = entity;
                }

                return entity;
            }
        }

        public void Update(T entity)
        {
            var operation = TableOperation.InsertOrReplace(entity);
            Table.Execute(operation);
        }

        public async Task UpdateAsync(T entity)
        {
            var operation = TableOperation.InsertOrReplace(entity);
            await Table.ExecuteAsync(operation);
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

        private static DateTimeKey GetCurrentParitionKey() => new DateTimeKey(DateTimeOffset.UtcNow, DateTimeKeyFlags.Date);

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
