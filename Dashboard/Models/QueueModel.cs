using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    public sealed class JobQueueModel
    {
        public string JobName { get; set; }
        public int JobCount { get; set; }
        public TimeSpan AverageTime { get; set; }
        public TimeSpan MedianTime { get; set; }
        public TimeSpan MaxTime { get; set; }
        public TimeSpan Mintime { get; set; }
        public List<JobQueueSummary> Jobs { get; set; } = new List<JobQueueSummary>();
    }

    public sealed class JobQueueSummary
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public TimeSpan QueueTime { get; set; }
    }
}