using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    /// <summary>
    /// Information about successful test queries returned to clients.
    /// </summary>
    public struct TestQueryStats
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
