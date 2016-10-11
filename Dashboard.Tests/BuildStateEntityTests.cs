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
    public class BuildStateEntityTests
    {
        [Fact]
        public void PreferHostRaw()
        {
            var jobId = JobId.ParseName("test");
            var host = new Uri("https://example.com");
            var entity = new BuildStateEntity()
            {
                JobName = jobId.Name,
                BuildNumber = 42,
                HostRaw = host.ToString(),
                HostName = "ignore"
            };

            Assert.Equal(host, entity.BoundBuildId.Host);
        }

        /// <summary>
        /// Plenty of legacy entities that don't have a HostRaw field.  Need to use host name in that case.
        /// </summary>
        [Fact]
        public void FallbackToHostName()
        {
            var jobId = JobId.ParseName("test");
            var host = new Uri("http://example.com");
            var entity = new BuildStateEntity()
            {
                JobName = jobId.Name,
                BuildNumber = 42,
                HostRaw = null,
                HostName = "example.com"
            };

            Assert.Equal(host, entity.BoundBuildId.Host);
        }
    }
}
