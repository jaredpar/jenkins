using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    public sealed class AllJobsModel
    {
        public List<string> Names { get; } = new List<string>();
    }

    public struct JobDaySummary
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public int Aborted { get; set; }
    }

    public sealed class JobSummary
    {
        public string Name { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public List<JobDaySummary> JobDaySummaryList { get; set; } = new List<JobDaySummary>();
    }
}