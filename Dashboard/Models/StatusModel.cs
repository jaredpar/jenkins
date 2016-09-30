using System;

namespace Dashboard.Models
{
    /// <summary>
    /// Captures the data that is used to compare test run times on a given date.  All times 
    /// are averages of the category on the given date.
    /// </summary>
    public sealed class TestRunComparison
    {
        public DateTime Date { get; set; }
        public TimeSpan AverageTimeCached { get; set; }
        public TimeSpan AverageTimeNoCached { get; set; }
        public TimeSpan AverageTimeAll { get; set; }

        public TimeSpan TimeSaved { get; set; }

        /// <summary>
        /// Total count of runs on this date.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Count of runs with a high (> 50%) cache count.
        /// </summary>
        public int CountHighCached { get; set; }

        /// <summary>
        /// Count of runs with any cache hits.
        /// </summary>
        public int CountCached { get; set; }

        /// <summary>
        /// Count of runs with no cache hits.
        /// </summary>
        public int CountNoCached { get; set; }
    }
}