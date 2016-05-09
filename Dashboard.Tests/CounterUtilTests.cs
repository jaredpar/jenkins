using Dashboard.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dashboard.Tests
{
    public abstract class CounterUtilTests
    {
        public sealed class GetRowKeyTests : CounterUtilTests
        {
            [Fact]
            public void DifferentForInterval()
            {
                var dateTime = DateTimeOffset.UtcNow;
                var counterData = new CounterData(dateTime, "hello world", isJenkins: false);
                var counterData2 = new CounterData(dateTime.AddMinutes(CounterUtil.MinuteInternal), counterData.EntityWriterId, isJenkins: counterData.IsJenkins);
                var key1 = CounterUtil.GetRowKey(counterData);
                var key2 = CounterUtil.GetRowKey(counterData);
                var key3 = CounterUtil.GetRowKey(counterData2);
                Assert.Equal(key1, key2);
                Assert.NotEqual(key1, key3);
            }

            [Fact]
            public void DifferentForJenkins()
            {
                var dateTime = DateTimeOffset.UtcNow;
                var counterData = new CounterData(dateTime, "hello world", isJenkins: false);
                var counterData2 = new CounterData(dateTime, counterData.EntityWriterId, isJenkins: true);
                var key1 = CounterUtil.GetRowKey(counterData);
                var key2 = CounterUtil.GetRowKey(counterData);
                var key3 = CounterUtil.GetRowKey(counterData2);
                Assert.Equal(key1, key2);
                Assert.NotEqual(key1, key3);
            }
        }

        public sealed class Query : CounterUtilTests, IDisposable
        {
            private sealed class TestCounterEntity : CounterEntity
            {
                public int Count { get; set; }

                public TestCounterEntity()
                {

                }

                public TestCounterEntity(CounterData counterData) : base(counterData)
                {

                }
            }

            private readonly CloudTable _table;
            private readonly string _entityWriterId = Guid.NewGuid().ToString();

            public Query()
            {
                var account = Util.GetStorageAccount();
                var client = account.CreateCloudTableClient();
                var name = $"CounterUtilTests{DateTime.Now.Ticks}";
                _table = client.GetTableReference(name);
                _table.Create();
            }

            public void Dispose()
            {
                _table.Delete();
            }

            private CounterData GetCounterData(DateTimeOffset dateTime, bool isJenkins = false)
            {
                return new CounterData(dateTime, _entityWriterId, isJenkins);
            }

            [Fact]
            public void SingleItem()
            {
                var dateTime = DateTimeOffset.UtcNow;
                var counterData = GetCounterData(dateTime);
                var entity = new TestCounterEntity(counterData)
                {
                    Count = 42
                };

                _table.Execute(TableOperation.Insert(entity));

                var list = CounterUtil.Query<TestCounterEntity>(_table, dateTime, dateTime).ToList();
                Assert.Equal(1, list.Count);
                Assert.Equal(42, list[0].Count);
            }

            [Fact]
            public void MultipleMinuteIntervals()
            {
                var dateTime1 = DateTimeOffset.UtcNow;
                var entity1 = new TestCounterEntity(GetCounterData(dateTime1))
                {
                    Count = 42
                };
                _table.Execute(TableOperation.Insert(entity1));

                var dateTime2 = dateTime1.AddMinutes(CounterUtil.MinuteInternal);
                var entity2 = new TestCounterEntity(GetCounterData(dateTime2))
                {
                    Count = 13
                };
                _table.Execute(TableOperation.Insert(entity2));

                var list = CounterUtil.Query<TestCounterEntity>(_table, dateTime1, dateTime2).ToList();
                Assert.Equal(2, list.Count);
                Assert.Equal(42, list[0].Count);
                Assert.Equal(13, list[1].Count);
            }

            [Fact]
            public void MultipleDayIntervals()
            {
                var dateTime1 = DateTimeOffset.UtcNow;
                var entity1 = new TestCounterEntity(GetCounterData(dateTime1))
                {
                    Count = 42
                };
                _table.Execute(TableOperation.Insert(entity1));

                var dateTime2 = dateTime1.AddDays(1);
                var entity2 = new TestCounterEntity(GetCounterData(dateTime2))
                {
                    Count = 13
                };
                _table.Execute(TableOperation.Insert(entity2));

                var list = CounterUtil.Query<TestCounterEntity>(_table, dateTime1, dateTime2).ToList();
                Assert.Equal(2, list.Count);
                Assert.Equal(42, list[0].Count);
                Assert.Equal(13, list[1].Count);
            }
        }
    }
}
