using Dashboard.Azure;
using Dashboard.Jenkins;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    public class BuildResultSummaryModel
    {
        public bool IncludePullRequests { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public int Limit { get; set; }
        public JobId JobId { get; set; }
        public List<BuildResultSummaryEntry> Entries { get; set; } = new List<BuildResultSummaryEntry>();
    }

    public class BuildResultSummaryEntry
    {
        public JobId JobId { get; set; }
        public int Count { get; set; }
    }

    public class BuildResultModel
    {
        public bool IncludePullRequests { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public JobId JobId { get; set; }
        public List<BuildResultEntity> Entries { get; set; } = new List<BuildResultEntity>();
    }

    /// <summary>
    /// Grouping of builds by categorization of the result they had: build failure, succeeded, test failure, etc ...
    /// </summary>
    public class BuildViewSummaryModel
    {
        public bool IncludePullRequests { get; set; }
        public bool IncludeSucceeded { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public List<BuildViewModel> Builds { get; set; } = new List<BuildViewModel>();
    }

    /// <summary>
    /// Build summary and the count of occurences that it had. 
    /// </summary>
    public class BuildViewModel
    {
        public string KindName;
        public int Count;
    }

    public class BuildResultKindModel
    {
        public bool IncludePullRequests { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public string ClassificationKind { get; set; }
        public List<BuildResultEntity> Entries { get; set; } = new List<BuildResultEntity>();
    }

    public class TestFailureSummaryModel
    {
        public bool IncludePullRequests { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public int Limit { get; set; }
        public List<TestFailureSummaryEntry> Entries { get; set; } = new List<TestFailureSummaryEntry>();
    }

    public class TestFailureSummaryEntry
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class TestFailureModel
    {
        public string Name { get; set; }
        public bool IncludePullRequests { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public List<BuildFailureEntity> Builds { get; } = new List<BuildFailureEntity>();
    }

    public class TestFailureData
    {
        public string Name { get; set; }
        public int TotalFailures { get; set; }
        public int PullRequestFailures { get; set; }
        public int CommitFailures { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<BuildData> Builds { get; set; }
    }

    public class BuildData
    {
        public string JobName { get; set; }
        public string JobShortName { get; set; }
        public string JobUri { get; set; }
        public string MachineName { get; set; }
        public DateTimeOffset DateTime { get; set; }
    }

    public class DemandRunStatusModel
    {
        public string UserName { get; set; }
        public string Commit { get; set; }
        public List<DemandBuildStatusModel> StatusList { get; set; } = new List<DemandBuildStatusModel>();
    }

    public class DemandBuildStatusModel
    {
        public string JobName { get; set; }
        public int? BuildNumber { get; set; }
        public int QueueNumber { get; set; }
    }
}