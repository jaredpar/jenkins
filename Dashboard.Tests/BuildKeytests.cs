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
    public class BuildKeyTests
    {
        [Fact]
        public void RoundTrip()
        {
            var buildId = new BuildId(42, JobId.ParseName("hello"));
            var key1 = new BuildKey(buildId);
            var key2 = BuildKey.Parse(key1.Key);
            Assert.Equal(key1, key2);
        }
    }
}
