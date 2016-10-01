using System;

namespace Dashboard.Azure.TestResults
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
