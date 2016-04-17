using Dashboard.Azure;
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
        [Fact]
        public void NormalizeKey()
        {
            Assert.Equal("foo", AzureUtil.NormalizeKey("foo", '_'));
            Assert.Equal("foo_bar", AzureUtil.NormalizeKey("foo/bar", '_'));
        }
    }
}
