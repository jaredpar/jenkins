using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Sql
{
    /// <summary>
    /// Information about the hit statistics including number of assembly, test passed, failed, etc ...
    /// </summary>
    public struct TestHitStats
    {
        public int AssemblyCount { get; }
        public int TestsPassed { get; }
        public int TestsFailed { get; }
        public int TestsSkipped { get; }
        public int TestsTotal => TestsPassed + TestsFailed + TestsSkipped;
        public TimeSpan Elapsed { get; }

        public TestHitStats(
            int assemblyCount,
            int testsPassed,
            int testsFailed,
            int testsSkipped,
            TimeSpan elapsed)

        {
            AssemblyCount = assemblyCount;
            TestsPassed = testsPassed;
            TestsFailed = testsFailed;
            TestsSkipped = testsSkipped;
            Elapsed = elapsed;
        }
    }
}
