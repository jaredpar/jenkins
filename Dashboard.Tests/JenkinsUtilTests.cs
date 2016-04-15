using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dashboard.Jenkins.Tests
{
    public class JenkinsUtilTests
    {
        public sealed class ConvertJobIdPath
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

        [Fact]
        public void GetJobPath()
        {
            Assert.Equal("job/test", JenkinsUtil.GetJobIdPath(JobId.ParseName("test")));
            Assert.Equal("job/test/job/op", JenkinsUtil.GetJobIdPath(JobId.ParseName("test/op")));
        }
    }
}
