using Dashboard.Azure;
using Dashboard.Azure.Builds;
using Dashboard.Jenkins;
using System;
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

        [Fact]
        public void ComplexJobKeyExact()
        {
            var jobId = JobId.ParseName("job/cat/job/dog");
            var buildId = new BuildId(42, jobId);
            var entityKey = BuildFailureEntity.GetExactEntityKey(buildId, "terrible/blah");
            Assert.False(AzureUtil.IsIllegalKey(entityKey.PartitionKey));
            Assert.False(AzureUtil.IsIllegalKey(entityKey.RowKey));
        }

        [Fact]
        public void ComplexJobKeyDate()
        {
            var jobId = JobId.ParseName("job/cat/job/dog");
            var buildId = new BuildId(42, jobId);
            var entityKey = BuildFailureEntity.GetDateEntityKey(DateTimeOffset.UtcNow, buildId, "terrible/blah");
            Assert.False(AzureUtil.IsIllegalKey(entityKey.PartitionKey));
            Assert.False(AzureUtil.IsIllegalKey(entityKey.RowKey));
        }
    }
}
