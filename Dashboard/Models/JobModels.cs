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

    public sealed class JobDaySummary
    {
        public DateTime Date { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public int Aborted { get; set; }
    }

    public sealed class JobModel
    {
        public string Name { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public List<Tuple<DateTime, TimeSpan>> DailyAverageDuration { get; } = new List<Tuple<DateTime, TimeSpan>>();
        public List<JobDaySummary> JobDaySummaryList { get; } = new List<JobDaySummary>();
    }
}