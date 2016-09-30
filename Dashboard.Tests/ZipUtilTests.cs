﻿using Xunit;

namespace Dashboard.Jenkins.Tests
{
    public sealed class ZipUtilTests
    {
        [Fact]
        public void RoundTripSimple()
        {
            var dataList = new[]
            {
                "foo",
                "cat in the hat"
            };

            foreach (var cur in dataList)
            {
                var bytes = ZipUtil.CompressText(cur);
                var outText = ZipUtil.DecompressText(bytes);
                Assert.Equal(cur, outText);
            }
        }
    }
}
