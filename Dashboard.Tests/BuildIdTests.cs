using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            }
        }
    }
}
