using Dashboard.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dashboard.Tests
{
    public class CounterUtilTests : IDisposable
    {
        public sealed class TestEntity : TableEntity
        {
            public int Count { get; set; }
        }

        public CloudTable Table { get; }
        public CounterUtil<TestEntity> CounterUtil { get; }

        public CounterUtilTests()
        {
            var account = Util.GetStorageAccount();
            var tableClient = account.CreateCloudTableClient();
            var tableName = "CounterUtilTestTableName";
            Table = tableClient.GetTableReference(tableName);
            Table.Create();
            CounterUtil = new CounterUtil<TestEntity>(Table);
        }

        public void Dispose()
        {
            Table.Delete();
        }

        [Fact]
        public async Task Basic()
        {
            var entity = CounterUtil.GetEntity();
            Assert.Equal(0, entity.Count);
            entity.Count++;
            await CounterUtil.UpdateAsync(entity);

            var foundEntity = await AzureUtil.QueryAsync<TestEntity>(Table, entity.GetEntityKey());
            Assert.Equal(1, foundEntity.Count);
        }

        [Fact]
        public async Task BasicAcrossThreads()
        {
            var date = DateTimeOffset.UtcNow;
            var threadCount = 1;
            var hitCount = 0;
            var list = new List<Thread>();
            var mre = new ManualResetEvent(initialState: false);
            Exception failed = null;
            for (int i = 0; i < threadCount; i++)
            {
                var thread = new Thread(() =>
                {
                    Interlocked.Increment(ref hitCount);
                    mre.WaitOne();

                    try
                    {
                        var entity = CounterUtil.GetEntity();
                        entity.Count = 1;
                        CounterUtil.Update(entity);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Exchange(ref failed, ex);
                    }
                });

                thread.Start();
                list.Add(thread);
            }

            // Ensure every thread gets spawned hence is guaranteed to have a different
            // managed thread id.
            while (hitCount != threadCount)
            {
                Thread.Yield();
            }

            mre.Set();
            foreach (var thread in list)
            {
                thread.Join();
            }

            Assert.Null(failed);

            var all = await CounterUtil.QueryAsync(date);
            Assert.Equal(threadCount, all.Count);
            Assert.Equal(threadCount, all.Sum(x => x.Count));
        }

        [Fact]
        public async Task QuerySimple()
        {
            var date = DateTimeOffset.Parse("2016/01/02");
            var key = DateTimeKey.GetDateKey(date);
            var entity = new TestEntity()
            {
                PartitionKey = key,
                RowKey = Guid.NewGuid().ToString("N"),
                Count = 42
            };

            await Table.ExecuteAsync(TableOperation.Insert(entity));
            var list = await CounterUtil.QueryAsync(date, date);
            Assert.Equal(1, list.Count);
            Assert.Equal(42, list[0].Count);
        }
    }
}
