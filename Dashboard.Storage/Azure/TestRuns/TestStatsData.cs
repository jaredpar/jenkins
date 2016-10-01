namespace Dashboard.Azure.TestRuns
{
    /// <summary>
    /// TODO: Does this belong here?
    /// </summary>
    public class TestCacheStatSummary
    {
        public TestQueryStats HitStats { get; }

        public int MissCount { get; }
        public int UploadCount { get; }

        /// <summary>
        /// Count of actively stored <see cref="TestResult"/> values
        /// </summary>
        public int TestResultCount { get; }

        /// <summary>
        /// Number of test runs in the specified time period.
        /// </summary>
        public int TestRunCount { get; }

        public TestCacheStatSummary(TestQueryStats hitStats, int missCount, int uploadCount, int testResultCount, int testRunCount)
        {
            HitStats = hitStats;
            MissCount = missCount;
            UploadCount = uploadCount;
            TestResultCount = testResultCount;
            TestRunCount = testRunCount;
        }
    }
}
