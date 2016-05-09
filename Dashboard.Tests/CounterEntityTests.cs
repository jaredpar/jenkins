using Dashboard.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dashboard.Tests
{
    public class CounterEntityTests
    {
        private sealed class TestCounterEntity : CounterEntity
        {
            internal TestCounterEntity(CounterData data) : base(data)
            {
            }
        }

        [Fact]
        public void Simple()
        {
            var dateTime = DateTimeOffset.UtcNow;
            var data = new CounterData(
                dateTime,
                "hello world",
                isJenkins: false);
            var entity = new TestCounterEntity(data);
            Assert.Equal(dateTime, entity.DateTime);
            Assert.Equal(data.EntityWriterId, entity.EntityWriterId);
            Assert.False(data.IsJenkins);
        }

        [Fact]
        public void PartitionKey()
        {
            var dateTime = DateTimeOffset.UtcNow;
            var data = new CounterData(
                dateTime,
                "hello world",
                isJenkins: false);
            var key = new DateKey(dateTime);
            var entity = new TestCounterEntity(data);
            Assert.Equal(key.Key, entity.PartitionKey);
        }
    }
}
