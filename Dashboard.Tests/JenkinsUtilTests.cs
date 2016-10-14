using Xunit;

namespace Dashboard.Jenkins.Tests
{
    public class JenkinsUtilTests
    {
        public sealed class ConvertJobIdPath : JenkinsUtilTests 
        {
            private static void Test(string path, JobId id)
            {
                Assert.Equal(path, JenkinsUtil.ConvertJobIdToPath(id));
                Assert.Equal(id, JenkinsUtil.ConvertPathToJobId(path));
            }

            [Fact]
            public void Root()
            {
                Test("", JobId.Root);
            }

            [Fact]
            public void Single()
            {
                Test("job/test", new JobId("test"));
            }

            [Fact]
            public void Nested()
            {
                Test("job/op/job/test", JobId.ParseName("op/test"));
            }
        }

        public sealed class ConvertBuildIdPath : JenkinsUtilTests
        {
            private static void Test(string path, BuildId buildId)
            {
                Assert.Equal(buildId, JenkinsUtil.ConvertPathToBuildId(path));
            }

            [Fact]
            public void Simple()
            {
                Test("job/dog/4", new BuildId(4, JobId.ParseName("dog")));
                Test("job/dog/4/", new BuildId(4, JobId.ParseName("dog")));
            }

            [Fact]
            public void Nested()
            {
                Test("job/house/job/dog/4", new BuildId(4, JobId.ParseName("house/dog")));
                Test("job/house/job/dog/4/", new BuildId(4, JobId.ParseName("house/dog")));
            }
        }

        public sealed class MiscTest : JenkinsUtilTests
        {
            [Fact]
            public void GetJobPath()
            {
                Assert.Equal("job/test", JenkinsUtil.GetJobPath(JobId.ParseName("test")));
                Assert.Equal("job/test/job/op", JenkinsUtil.GetJobPath(JobId.ParseName("test/op")));
            }

            [Fact]
            public void GetBuildPath()
            {
                Assert.Equal("job/test/2/", JenkinsUtil.GetBuildPath(new BuildId(2, JobId.ParseName("test"))));
                Assert.Equal("job/test/job/op/13/", JenkinsUtil.GetBuildPath(new BuildId(13, JobId.ParseName("test/op"))));
            }

            [Fact]
            public void ConvertTimestamp()
            {
                var dateTimeOffset = JenkinsUtil.ConvertTimestampToDateTimeOffset(1462922992229);
                Assert.Equal(635985197922290000, dateTimeOffset.UtcTicks);
            }
        }
    }
}
