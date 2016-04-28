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
                "test",
                BuildResultKind.BuildFailure,
                prInfo: null);
            Assert.Equal(buildId, entity.BuildId);
            Assert.Equal(buildId.Id, entity.BuildNumber);
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
                "test",
                BuildResultKind.BuildFailure,
                prInfo: prInfo);
            Assert.True(entity.HasPullRequestInfo);
            Assert.Equal(entity.PullRequestInfo.Author, prInfo.Author);
            Assert.Equal(entity.PullRequestInfo.AuthorEmail, prInfo.AuthorEmail);
            Assert.Equal(entity.PullRequestInfo.Id, prInfo.Id);
            Assert.Equal(entity.PullRequestInfo.PullUrl, prInfo.PullUrl);
            Assert.Equal(entity.PullRequestSha1, prInfo.Sha1);
        }
    }
}
