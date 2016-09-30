using Dashboard.Azure;
using Dashboard.Jenkins;
using Xunit;

namespace Dashboard.Tests
{
    public class AzureUtilTests
    {
        public class ViewNameTests : AzureUtilTests
        {
            [Fact]
            public void Root()
            {
                Assert.Equal(AzureUtil.ViewNameRoot, AzureUtil.GetViewName(JobId.Root));
            }

            [Fact]
            public void Simple()
            {
                var jobId = JobId.ParseName("dog");
                Assert.Equal(AzureUtil.ViewNameRoot, AzureUtil.GetViewName(jobId));
            }

            [Fact]
            public void Nested()
            {
                var jobId = JobId.ParseName("house/dog");
                Assert.Equal("house", AzureUtil.GetViewName(jobId));
            }

            [Fact]
            public void VeryNested()
            {
                var jobId = JobId.ParseName("house/dog/lab");
                Assert.Equal("house", AzureUtil.GetViewName(jobId));
            }

            [Fact]
            public void Private()
            {
                var jobId = JenkinsUtil.ConvertPathToJobId("job/Private/job/dotnet_debuggertests/job/master/job/linux_dbg/");
                Assert.Equal("dotnet_debuggertests", AzureUtil.GetViewName(jobId));
            }

            /// <summary>
            /// Roslyn is a special case here.  We group the public and private jobs together.
            /// </summary>
            [Fact]
            public void PrivateRoslyn()
            {
                var jobId = JenkinsUtil.ConvertPathToJobId("job/Private/job/dotnet_roslyn-internal/job/master/job/windows_debug_eta/");
                Assert.Equal("dotnet_roslyn", AzureUtil.GetViewName(jobId));
            }
        }

        public class MiscTests : AzureUtilTests
        {
            [Fact]
            public void NormalizeKey()
            {
                Assert.Equal("foo", AzureUtil.NormalizeKey("foo", '_'));
                Assert.Equal("foo_bar", AzureUtil.NormalizeKey("foo/bar", '_'));
            }
        }
    }
}
