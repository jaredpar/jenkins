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
    public class BuildResultEntityTests
    {
        [Fact]
        public void Properties()
        {
            var buildId = new BuildId(42, JobId.ParseName("hello"));
            var buildDate = DateTimeOffset.UtcNow;
            var entity = new BuildResultEntity(
                buildId,
                buildDate,
                TimeSpan.FromSeconds(1),
                "kind",
                "test",
                BuildResultClassification.Succeeded,
                prInfo: null);
            Assert.Equal(BuildResultClassification.Succeeded.Kind, entity.Classification.Kind);
            Assert.Equal(BuildResultClassification.Succeeded.Name, entity.Classification.Name);
            Assert.Equal(buildId, entity.BuildId);
            Assert.Equal(buildId.Number, entity.BuildNumber);
            Assert.Equal(buildId.JobId, entity.JobId);
            Assert.Equal(buildDate, entity.BuildDateTimeOffset);
            Assert.Equal("test", entity.MachineName);
            Assert.False(entity.HasPullRequestInfo);
            Assert.Null(entity.PullRequestInfo);
        }

        [Fact]
        public void PullRequestInfo()
        {
            var buildId = new BuildId(42, JobId.ParseName("hello"));
            var buildDate = DateTimeOffset.UtcNow;
            var prInfo = new PullRequestInfo("bob", "dog", 42, "cat", "tree");
            var entity = new BuildResultEntity(
                buildId,
                buildDate,
                TimeSpan.FromSeconds(1),
                "kind",
                "test",
                BuildResultClassification.Succeeded,
                prInfo: prInfo);
            Assert.True(entity.HasPullRequestInfo);
            Assert.Equal(entity.PullRequestInfo.Author, prInfo.Author);
            Assert.Equal(entity.PullRequestInfo.AuthorEmail, prInfo.AuthorEmail);
            Assert.Equal(entity.PullRequestInfo.Id, prInfo.Id);
            Assert.Equal(entity.PullRequestInfo.PullUrl, prInfo.PullUrl);
            Assert.Equal(entity.PullRequestSha1, prInfo.Sha1);
        }

        [Fact]
        public void ComplexJobKey()
        {
            var jobId = JobId.ParseName("job/cat/job/dog");
            var buildId = new BuildId(42, jobId);
            var entityKey = BuildResultEntity.GetExactEntityKey(buildId);
            Assert.False(AzureUtil.IsIllegalKey(entityKey.PartitionKey));
            Assert.False(AzureUtil.IsIllegalKey(entityKey.RowKey));
        }

        [Fact]
        public void ViewNameAll()
        {
            var buildId = new BuildId(42, JobId.ParseName("test"));
            var entity = new BuildResultEntity(buildId, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), "kind", "test", BuildResultClassification.Succeeded, null);
            Assert.Equal(AzureUtil.ViewNameRoot, entity.ViewName);
        }

        [Fact]
        public void ViewNameOther()
        {
            var buildId = new BuildId(42, JobId.ParseName("house/test"));
            var entity = new BuildResultEntity(buildId, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), "kind", "test", BuildResultClassification.Succeeded, null);
            Assert.Equal("house", entity.ViewName);
        }
    }
}
