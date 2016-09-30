using Dashboard.Azure;
using Dashboard.Azure.Builds;
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
        public void JobNameInFolder()
        {
            var jobId = JobId.ParseName("job/cat/job/dog");
            var buildId = new BuildId(42, jobId);
            var buildKey = new BuildKey(buildId);
            Assert.False(AzureUtil.IsIllegalKey(buildKey.Key));
        }

        [Fact]
        public void Simple()
        {
            var jobId = JobId.ParseName("dog");
            var buildId = new BuildId(42, jobId);
            var buildKey = new BuildKey(buildId);
            Assert.False(AzureUtil.IsIllegalKey(buildKey.Key));
            Assert.Equal("42-dog", buildKey.Key);
        }
    }
}
