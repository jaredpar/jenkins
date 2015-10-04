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

    public sealed class JobModel
    {
        public string Name { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public List<Tuple<DateTime, TimeSpan>> DailyAverageDuration { get; } = new List<Tuple<DateTime, TimeSpan>>();
    }

}