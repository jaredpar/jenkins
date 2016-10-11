using Dashboard.Jenkins;
using System;
using Xunit;

namespace Dashboard.Tests
{
    public class BoundBuildIdTests
    {
        public sealed class EqualityTests : BoundBuildIdTests
        {
            private static void RunAll(EqualityUnit<BoundBuildId> unit)
            {
                unit.RunAll(
                    compEqualsOperator: (x, y) => x == y,
                    compNotEqualsOperator: (x, y) => x != y);
            }

            [Fact]
            public void HostName()
            {
                var host = new Uri("http://test.com");
                var buildId = new BuildId(42, JobId.ParseName("cat"));
                RunAll(EqualityUnit
                    .Create(new BoundBuildId(host, buildId))
                    .WithEqualValues(new BoundBuildId(host, buildId), new BoundBuildId(host, buildId))
                    .WithNotEqualValues(new BoundBuildId(new Uri("http://other.com"), buildId)));
            }
        }

        public sealed class NormalizeUriTests : BoundBuildIdTests
        {
            [Fact]
            public void Simple()
            {
                var uri = new Uri("https://example.com");
                Assert.Equal(uri, BoundBuildId.NormalizeHostUri(uri));
            }

            [Fact]
            public void SimpleWithPort()
            {
                var uri = new Uri("https://example.com:400");
                Assert.Equal(uri, BoundBuildId.NormalizeHostUri(uri));
            }

            [Fact]
            public void CaseFixup()
            {
                var uri = new Uri("https://example.com");
                Assert.Equal(new Uri("https://example.com"), BoundBuildId.NormalizeHostUri(uri));
            }

            [Fact]
            public void PathRemoval()
            {
                var uri = new Uri("https://example.com/again");
                Assert.Equal(new Uri("https://example.com"), BoundBuildId.NormalizeHostUri(uri));
            }
        }
    }
}
