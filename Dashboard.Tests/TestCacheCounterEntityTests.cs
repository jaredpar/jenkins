using Dashboard.Azure;
using Dashboard.Azure.TestResults;
using System;
using Xunit;

namespace Dashboard.Tests
{
    public sealed class TestCacheCounterEntityTests
    {
        [Fact]
        public void EntityWriterId()
        {
            var id = Guid.NewGuid().ToString();
            var counterData = new CounterData(id, isJenkins: true);
            var entity = new TestCacheCounterEntity(counterData);
            Assert.Equal(id, entity.EntityWriterId);
        }
    }
}
