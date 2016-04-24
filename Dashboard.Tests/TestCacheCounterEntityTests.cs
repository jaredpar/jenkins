using Dashboard.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dashboard.Tests
{
    public sealed class TestCacheCounterEntityTests
    {
        [Fact]
        public void EntityWriterId()
        {
            var id = Guid.NewGuid().ToString();
            var entity = TestCacheCounterEntity.Create(DateTime.UtcNow, id, isJenkins: true);
            Assert.Equal(id, entity.EntityWriterId);
        }
    }
}
