using Roslyn.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    public enum JobListContainerKind
    {
        Root,
        View,
        Job
    }

    public sealed class JobListModel
    {
        public string ContainerName { get; set; }
        public JobListContainerKind Kind { get; set; }
        public List<JobId> NestedList { get; set; } = new List<JobId>();
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
        public List<JobId> NestedJobIdList { get; set; } = new List<JobId>();
    }

    public sealed class ViewModel
    {
        public List<ViewInfo> Views { get; }

        public ViewModel(List<ViewInfo> views)
        {
            Views = views;
        }
    }
}