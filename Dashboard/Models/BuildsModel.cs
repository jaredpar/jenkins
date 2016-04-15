using Dashboard.Azure;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    public class BuildFailureSummary
    {
        public bool IncludePullRequests { get; set; }
        public DateTime StartDate { get; set; }
        public int Limit { get; set; }
        public List<BuildFailureEntry> Entries { get; set; } = new List<BuildFailureEntry>();
    }

    public class BuildFailureEntry
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class BuildFailureModel
    {
        public string Name { get; set; }
        public bool IncludePullRequests { get; set; }
        public DateTime StartDate { get; set; }
        public List<BuildFailureEntity> Builds { get; } = new List<BuildFailureEntity>();
    }
}