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
