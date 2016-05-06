using Dashboard.Azure;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dashboard.Tests
{
    public class BuildFailureEntityTests
    {
        [Fact]
        public void DateKey()
        {
            var offset = DateTimeOffset.UtcNow;
            var buildId = new BuildId(42, JobId.ParseName("test"));
            var key = BuildFailureEntity.GetDateEntityKey(offset, buildId, "testName");
            Assert.False(key.RowKey.Contains("Key"));
        }

    }
}
