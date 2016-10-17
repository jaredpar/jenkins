using Dashboard.Azure.Builds;
using Dashboard.Jenkins;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Dashboard.Models
{
    public sealed class BuildFilterModel
    {
        public bool IncludePullRequests { get; set; }
        /// <summary>
        /// Whether to include results from flow jobs/runs
        /// The elapsed time of a flow job/run is the sum of elapsed time of all its sub jobs/runs.
        /// They should be excluded from elapsed time calcuation, as they do NOT consume additional machine resources.
        /// </summary>
        public bool IncludeFlowRunResults { get; set; }
        public bool DisplayFlowRunCheckBox { get; set; }
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
        public string JobKind { get; set; }
        public int ElapsedTime { get; set; }
        public ClassificationKind ClassificationKind { get; set; }
    }

    /// <summary>
    /// List of elapsed time by categorization of their ranges (0 ~ 100s), (100 ~ 1000s) ...
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
        /// List elements at 0 represents the range of 0 ~ 10s
        /// List elements at 1 represents the range of 10 ~ 100s
        /// Length of this array is set to _ETRangeCount, which counts runs whose ET is in range as much as 100000 ~ 1000000s
        /// </summary>
        /// </summary>
        public List<int> RunCountsPerETRange { get; set; } = new List<int>();
    }

    /// <summary>
    /// Team/Project repo name and the sum of the elapsed time of all their jobs
    /// </summary>
    public class ProjectElapsedTimeModel
    {
        public string RepoName { get; set; }
        public int ETSum { get; set; }
    }

    /// <summary>
    /// List of projects ranked by the sum of elapsed time of all their jobs
    /// </summary>
    public class ProjectElapsedTimeSummaryModel
    {
        public BuildFilterModel Filter { get; set; }

        /// <summary>
        /// Total number of builds.  Includes the count of succeeded builds even if <see cref="IncludeSucceeded"/>
        /// is false.
        /// </summary>
        public int TotalBuildCount { get; set; }

        /// <summary>
        /// Total number of flow jobs.
        /// </summary>
        public int FlowJobCount { get; set; }

        /// <summary>
        /// Total number of builds that succeeded.
        /// </summary>
        public int TotalSucceededCount { get; set; }

        /// <summary>
        /// Sum of elapsed time of all the jobs of every git repo.
        /// </summary>
        /// </summary>
        public List<ProjectElapsedTimeModel> ProjectElapsedTimeList { get; set; } = new List<ProjectElapsedTimeModel>();
    }

    /// <summary>
    /// Aggregated Job ET entry.
    /// 1st element is the ET sum of all its runs (each run has a different build number)
    /// 2nd element is the # of its runs
    /// </summary>
    public class AgJobElapsedTime
    {
        public int ETSum { get; set; }
        public int NumOfBuilds { get; set; }
    }

    /// <summary>
    /// Job ET list of selected repo, ranked from job with most ET to least.
    /// </summary>
    public class JobElapsedTimeModel
    {
        public BuildFilterModel Filter { get; set; }

        /// <summary>
        /// Total number of jobs
        /// Note even though each job can have multiple builds, its job count is still 1.
        /// </summary>
        public int TotalJobCount { get; set; }

        /// <summary>
        /// Total number of runs
        /// Note even though each job can have multiple runs/builds
        /// If "Include Flow Run/Job Results" are not checked, the # of flow runs will NOT be counted.
        /// </summary>
        public int TotalRunCount { get; set; }

        /// <summary>
        /// Total number of flow runs.
        /// </summary>
        public int FlowRunCount { get; set; }

        /// <summary>
        /// Total elapsed time of current repo
        /// </summary>
        public int TotalETOfCurrRepo { get; set; }

        /// <summary>
        /// Aggregated map of job elapsed time, where key is the job name.
        /// Elapsed time from runs of the same job (but different build IDs) are summed up.
        /// </summary>
        /// </summary>
        public SortedDictionary<string, AgJobElapsedTime> AgJobElapsedTimeDict { get; set; } = new SortedDictionary<string, AgJobElapsedTime>();
    }

    public class JobElapsedTimePerBuildModel
    {
        public BuildFilterModel Filter { get; set; }

        /// <summary>
        /// Total number of runs/builds of current job
        /// </summary>
        public int TotalBuildCount { get; set; }

        /// <summary>
        /// Total elapsed time of current job
        /// </summary>
        public int TotalETOfCurrJob { get; set; }

        public List<BuildResultEntity> Entries { get; set; } = new List<BuildResultEntity>();
    }

    public sealed class BuildStatusModel
    {
        public bool All { get; set; }
        public bool Error { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public List<BuildStateEntity> List { get; } = new List<BuildStateEntity>();

        public BuildStatusModel()
        {

        }

        public BuildStatusModel(
            bool all,
            bool error,
            DateTimeOffset startDate,
            List<BuildStateEntity> list)
        {
            All = all;
            Error = error;
            StartDate = startDate;
            List = list;
        }
    }

    public sealed class BuildStats
    {
        public DateTimeOffset Date { get; }
        public int BuildSucceededCount { get; set; }
        public int BuildFailedCount { get; set; }

        public int BuildCount => BuildSucceededCount + BuildFailedCount;

        public BuildStats(DateTimeOffset date)
        {
            Date = date;
        }
    }

    public sealed class BuildStatsModel
    {
        public List<BuildStats> BuildStats { get; }
        public bool IncludePullRequest { get; }

        public BuildStatsModel(List<BuildStats> buildStats, bool pr)
        {
            BuildStats = buildStats;
            IncludePullRequest = pr;
        }
    }
}