using Dashboard.Jenkins;
using Xunit;

namespace Dashboard.Tests
{
    public class JobKindTests
    {
        [Fact]
        public void IsWellKnown()
        {
            foreach (var cur in JobKind.All)
            {
                Assert.True(JobKind.IsWellKnown(cur));
            }
        }
    }
}
