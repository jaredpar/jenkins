using Dashboard.Azure;
using Dashboard.Jenkins;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    public sealed class BuildFilterModel
    {
        public bool IncludePullRequests { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public int? Limit { get; set; }
        public string Name { get; set; }
        public string ViewName { get; set; }
        public string ActionName { get; set; }

        public object GetRouteValues(string name = null)
        {
            return new
            {
                name = name ?? Name,
                viewName = ViewName,
                pr = IncludePullRequests,
                limit = Limit,
                startDate = StartDate.ToString("yyyy-MM-dd")
            };
        }
    }

    public class BuildResultSummaryModel
    {
        public BuildFilterModel Filter { get; set; }
        public List<BuildResultSummaryEntry> Entries { get; set; } = new List<BuildResultSummaryEntry>();
    }

    public class BuildResultSummaryEntry
    {
        public JobId JobId { get; set; }
        public int Count { get; set; }
    }

    public class BuildResultModel
    {
        public BuildFilterModel Filter { get; set; }
        public JobId JobId { get; set; }
        public List<BuildResultEntity> Entries { get; set; } = new List<BuildResultEntity>();
    }

    /// <summary>
    /// Grouping of builds by categorization of the result they had: build failure, succeeded, test failure, etc ...
    /// </summary>
    public class BuildViewSummaryModel
    {
        public BuildFilterModel Filter { get; set; }

        /// <summary>
        /// Total number of builds.  Includes the count of succeeded builds even if <see cref="IncludeSucceeded"/>
        /// is false.
        /// </summary>
        public int TotalBuildCount { get; set; }

        /// <summary>
        /// Total number of builds that succeeded.
        /// </summary>
        public int TotalSucceededCount { get; set; }

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

    public class BuildViewNameModel
    {
        public string ViewName;
        public int Count;
    }

    public class BuildResultKindByViewNameModel
    {
        public BuildFilterModel Filter { get; set; }
        public string ClassificationKind { get; set; }
        public List<BuildViewNameModel> Builds { get; set; } = new List<BuildViewNameModel>();
        public int TotalResultCount { get; set; }
    }

    public class BuildResultKindModel
    {
        public BuildFilterModel Filter { get; set; }
        public string ClassificationKind { get; set; }
        public List<BuildResultEntity> Entries { get; set; } = new List<BuildResultEntity>();
    }

    public class TestFailureSummaryModel
    {
        public BuildFilterModel Filter { get; set; }
        public List<TestFailureSummaryEntry> Entries { get; set; } = new List<TestFailureSummaryEntry>();
    }

    public class TestFailureSummaryEntry
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class TestFailureModel
    {
        public BuildFilterModel Filter { get; set; }
        public string Name { get; set; }
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

    /// <summary>
    /// Job info and its elapsed time (in seconds)
    /// </summary>
    public class ElapsedTimeModel
    {
        public JobId JobId { get; set; }
        public string JobName { get; set; }
        public int ElapsedTime;
    }

    /// <summary>
    /// List of elapsed time by categorization of their ranges (0 ~ 100ms), (100 ~ 1000ms) ...
    /// </summary>
    public class ElapsedTimeSummaryModel
    {
        public BuildFilterModel Filter { get; set; }

        /// <summary>
        /// Total number of builds.  Includes the count of succeeded builds even if <see cref="IncludeSucceeded"/>
        /// is false.
        /// </summary>
        public int TotalBuildCount { get; set; }

        /// <summary>
        /// Total number of builds that succeeded.
        /// </summary>
        public int TotalSucceededCount { get; set; }

        /// <summary>
        /// Counts of runs per elapsed time range.
        /// List elements at 0 represents the range of 0 ~ 10ms
        /// List elements at 1 represents the range of 10 ~ 100ms
        /// Length of this array is set to 6, which counts runs whose ET is in range as much as 100000 ~ 1000000ms
        /// </summary>
        /// </summary>
        public List<int> RunCountsPerETRange { get; set; } = new List<int>();
    }

    /// <summary>
    /// Team/Project repo name and the sum of the elapsed time of all their jobs
    /// </summary>
    public class RepoETModel
    {
        public string RepoName { get; set; }
        public int ETSum;
    }

    /// <summary>
    /// List of team/project repos ranked by the sum of elapsed time of all their jobs
    /// </summary>
    public class RepoETSummaryModel
    {
        public BuildFilterModel Filter { get; set; }

        /// <summary>
        /// Total number of builds.  Includes the count of succeeded builds even if <see cref="IncludeSucceeded"/>
        /// is false.
        /// </summary>
        public int TotalBuildCount { get; set; }

        /// <summary>
        /// Total number of builds that succeeded.
        /// </summary>
        public int TotalSucceededCount { get; set; }

        /// <summary>
        /// Sum of elapsed time of all the jobs of every git repo.
        /// </summary>
        /// </summary>
        public List<RepoETModel> RepoETList { get; set; } = new List<RepoETModel>();
    }
}