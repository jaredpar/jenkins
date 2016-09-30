using Dashboard.Jenkins;
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
                var buildId = new BuildId(42, JobId.ParseName("cat"));
                RunAll(EqualityUnit
                    .Create(new BoundBuildId("test", buildId))
                    .WithEqualValues(new BoundBuildId("test", buildId), new BoundBuildId("test", buildId))
                    .WithNotEqualValues(new BoundBuildId("other", buildId)));
            }
        }
    }
}
