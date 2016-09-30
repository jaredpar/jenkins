using Dashboard.Jenkins;
using Xunit;

namespace Dashboard.Tests
{
    public class BuildIdTests
    {
        public sealed class EqualityTests
        {
            private static void RunAll(EqualityUnit<BuildId> unit)
            {
                unit.RunAll(
                    compEqualsOperator: (x, y) => x == y,
                    compNotEqualsOperator: (x, y) => x != y);
            }

            [Fact]
            public void Number()
            {
                RunAll(EqualityUnit.Create(new BuildId(42, JobId.Root))
                    .WithEqualValues(new BuildId(42, JobId.Root))
                    .WithNotEqualValues(new BuildId(13, JobId.Root)));
            }

            [Fact]
            public void JobIdDifferent()
            {
                var id1 = JobId.ParseName("dog");
                var id2 = JobId.ParseName("cat");
                RunAll(EqualityUnit.Create(new BuildId(42, id1))
                    .WithEqualValues(new BuildId(42, id1))
                    .WithNotEqualValues(
                        new BuildId(42, id2),
                        new BuildId(42, JobId.Root)));
            }
        }
    }
}
