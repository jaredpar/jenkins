using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Roslyn.Jenkins.Tests
{
    public class JenkinsUtilTests
    {
        [Fact]
        public void RoundTrip()
        {
            /*
            foreach (var kind in JenkinsUtil.GetAllJobKinds())
            {
                var name = JenkinsUtil.GetJobName(kind);
                Assert.NotNull(name);
                Assert.Equal(kind, JenkinsUtil.GetJobKind(name));

                JobKind other;
                Assert.True(JenkinsUtil.TryGetJobKind(name, out other));
                Assert.Equal(kind, other);
            }
            */
        }
    }
}
