using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
