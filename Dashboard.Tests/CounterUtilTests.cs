﻿using Dashboard.Azure;
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
            var date = DateTimeOffset.UtcNow;
            await CounterUtil.UpdateAsync(entity =>
            {
                Assert.Equal(0, entity.Count);
                entity.Count++;
            });

            var foundEntity = await CounterUtil.QueryAsync(date);
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
                        CounterUtil.Update(e => e.Count = 1);
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
            Assert.Equal(threadCount, CounterUtil.ApproximateCacheCount);
        }

        [Fact]
        public async Task BasicFromFactory()
        {
            var date = DateTimeOffset.UtcNow;
            var factory = new CounterUtilFactory();
            var count = 10;
            for (var i =0; i< count; i++)
            {
                var util = factory.Create<TestEntity>(Table);
                util.Update(e => e.Count++);
            }

            var all = await CounterUtil.QueryAsync(date);

            // In general this should be 1.  However to account for the possibility the test 
            // was ran across a date boundary use <= 2
            Assert.True(all.Count <= 2);
            Assert.Equal(count, all.Sum(x => x.Count));
        }

        /// <summary>
        /// This is why the factory method is preferred.
        /// </summary>
        [Fact]
        public async Task BasicFromCtor()
        {
            var date = DateTimeOffset.UtcNow;
            var factory = new CounterUtilFactory();
            var count = 10;
            for (var i =0; i< count; i++)
            {
                var util = new CounterUtil<TestEntity>(Table);
                util.Update(e => e.Count++);
            } 

            var all = await CounterUtil.QueryAsync(date);

            // This is why a factory is preferred.  This method creates a new entity, and hence row,
            // per ctor.
            Assert.Equal(count, all.Count);
            Assert.Equal(count, all.Sum(x => x.Count));
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
