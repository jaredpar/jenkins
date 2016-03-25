using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    /// <summary>
    /// Captures the data that is used to compare test run times on a given date.  All times 
    /// are averages of the category on the given date.
    /// </summary>
    public sealed class TestRunComparison
    {
        public DateTime Date { get; set; }
        public TimeSpan FullCacheTime { get; set; }
        public TimeSpan ChunkOnlyTime { get; set; }
        public TimeSpan LegacyTime { get; set; }
    }
}