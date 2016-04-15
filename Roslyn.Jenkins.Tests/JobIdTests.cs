using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Roslyn.Jenkins.Tests
{
    public class JobIdTests
    {
        public sealed class Equality : JobIdTests
        {
            private static void RunAll(EqualityUnit<JobId> unit)
            {
                unit.RunAll(
                    compEqualsOperator: (x, y) => x == y,
                    compNotEqualsOperator: (x, y) => x != y);
            }

            [Fact]
            public void Root()
            {
                RunAll(EqualityUnit
                    .Create(JobId.Root)
                    .WithEqualValues(JobId.Root, JobId.ParseName(""))
                    .WithNotEqualValues(new JobId("test")));
            }

            [Fact]
            public void Single()
            {
                RunAll(EqualityUnit
                    .Create(new JobId("test"))
                    .WithEqualValues(new JobId("test"), JobId.ParseName("test"))
                    .WithNotEqualValues(new JobId("test2"), JobId.Root));
            }

            [Fact]
            public void Nested()
            {
                RunAll(EqualityUnit
                    .Create(new JobId("test", new JobId("op")))
                    .WithEqualValues(new JobId("test", new JobId("op")), JobId.ParseName("op/test"))
                    .WithNotEqualValues(new JobId("test"), JobId.Root, new JobId("op")));
            }
        }

        public class FullName : JobIdTests
        {
            private void TestAll(JobId id, string name)
            {
                var other = JobId.ParseName(name);
                Assert.Equal(id, other);
                Assert.Equal(id.Name, name);
            }

            [Fact]
            public void ParseRoot()
            {
                TestAll(JobId.Root, "");
            }

            [Fact]
            public void ParseSingle()
            {
                TestAll(new JobId("test"), "test");
                TestAll(new JobId("dog"), "dog");
            }

            [Fact]
            public void ParseNested()
            {
                TestAll(new JobId("test", new JobId("op")), "op/test");
            }
        }
    }
}
