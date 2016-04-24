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
        public int AssemblyCount { get; set; }
        public int TestsPassed { get; set; }
        public int TestsFailed { get; set; }
        public int TestsSkipped { get; set; }
        public long ElapsedSeconds { get; set; }

        public int TestsTotal => TestsPassed + TestsFailed + TestsSkipped;
        public TimeSpan Elapsed => TimeSpan.FromSeconds(ElapsedSeconds);
    }
}
