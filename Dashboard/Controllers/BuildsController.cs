using Dashboard.Models;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using Dashboard;
using Dashboard.Azure;
using Dashboard.Azure.Json;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Dashboard.Helpers;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Dashboard.Controllers
{
    public class BuildsController : Controller
    {
        private readonly DashboardStorage _storage;
        private readonly BuildUtil _buildUtil;
        private const int _ETRangeCount = 6;

        public BuildsController()
        {
            var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            _storage = new DashboardStorage(connectionString);
            _buildUtil = new BuildUtil(_storage.StorageAccount);
        }

        /// <summary>
        /// Lists all of the build failures.
        /// </summary>
        public ActionResult Index(bool pr = false, DateTimeOffset? startDate = null, int limit = 10)
        {
            return Test(name: null, pr: pr, startDate: startDate, limit: limit);
        }

        /// <summary>
        /// Summarize the details of an individual failure.
        /// </summary>
        public ActionResult Test(string name = null, string viewName = AzureUtil.ViewNameRoslyn, bool pr = false, DateTimeOffset? startDate = null, int limit = 10)
        {
            var filter = CreateBuildFilter(nameof(Test), name, viewName, pr, startDate, limit);
            if (name == null)
            {
                var model = GetTestFailureSummaryModel(filter);
                return View(viewName: "TestFailureList", model: model);
            }
            else
            {
                var model = GetTestFailureModel(filter);
                return View(viewName: "TestFailure", model: model);
            }
        }

        public ActionResult Result(string name = null, string viewName = AzureUtil.ViewNameRoslyn, bool pr = false, DateTime? startDate = null, int limit = 10)
        {
            var filter = CreateBuildFilter(nameof(Result), name, viewName, pr, startDate, limit);
            if (name == null)
            {
                var model = GetBuildResultSummaryModel(filter);
                return View(viewName: "BuildResultList", model: model);
            }
            else
            {
                var model = GetBuildResultModel(name, filter);
                return View(viewName: "BuildResult", model: model);
            }
        }

        public async Task<ActionResult> Status(bool all = false, DateTimeOffset? startDate = null)
        {
            var startDateValue = startDate ?? DateTimeOffset.UtcNow;
            var key = BuildStateEntity.GetPartitionKey(startDateValue);
            var table = _storage.StorageAccount.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.BuildState);
            var filter = TableQueryUtil.Column(
                nameof(TableEntity.PartitionKey),
                key,
                ColumnOperator.GreaterThanOrEqual);
            if (!all)
            {
                filter = TableQueryUtil.And(
                    filter,
                    TableQueryUtil.Column(nameof(BuildStateEntity.IsDataComplete), false));
            }
            var list = await AzureUtil.QueryAsync<BuildStateEntity>(table, filter);
            var model = new BuildStatusModel(all, startDateValue, list);

            return View(viewName: "Status", model: model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task StatusRefresh()
        {
            var key = BuildStateEntity.GetPartitionKey(DateTimeOffset.UtcNow);
            var table = _storage.StorageAccount.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.BuildState);
            var query = TableQueryUtil.PartitionKey(key);
            var list = await AzureUtil.QueryAsync<BuildStateEntity>(table, query);
            var queue = _storage.StorageAccount.CreateCloudQueueClient().GetQueueReference(AzureConstants.QueueNames.ProcessBuild);

            foreach (var entity in list.Where(x => !x.IsDataComplete))
            {
                var buildMessage = new BuildStateMessage(entity.BuildStateKey, entity.BoundBuildId);
                var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(buildMessage));
                await queue.AddMessageAsync(queueMessage);
            }
        }

        /// <summary>
        /// A view of the builds grouped by the result.
        /// </summary>
        /// <returns></returns>
        public ActionResult View(bool pr = false, DateTimeOffset? startDate = null, string viewName = AzureUtil.ViewNameRoslyn)
        {
            var filter = CreateBuildFilter(actionName: nameof(View), viewName: viewName, startDate: startDate, pr: pr);
            var results =
                _buildUtil.GetBuildResults(filter.StartDate, viewName)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId))
                .ToList();

            var totalCount = results.Count;
            var totalSucceeded = results.Count(x => x.ClassificationKind == ClassificationKind.Succeeded);

            var builds = results
                .Where(x => x.ClassificationKind != ClassificationKind.Succeeded)
                .GroupBy(x => x.ClassificationName)
                .Select(x => new BuildViewModel() { KindName = x.Key, Count = x.Count() })
                .ToList();

            var model = new BuildViewSummaryModel()
            {
                Filter = filter,
                TotalBuildCount = totalCount,
                TotalSucceededCount = totalSucceeded,
                Builds = builds
            };

            return View(viewName: "View", model: model);
        }

        /// <summary>
        /// A view of the elapsed time grouped by the result.
        /// </summary>
        /// <returns></returns>
        public ActionResult ElapsedTime(bool pr = false, DateTimeOffset? startDate = null, string viewName = AzureUtil.ViewNameRoslyn)
        {
            var filter = CreateBuildFilter(actionName: nameof(ElapsedTime), viewName: viewName, startDate: startDate, pr: pr);
            var results =
                _buildUtil.GetBuildResults(filter.StartDate, viewName)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId))
                .ToList();

            var totalCount = results.Count;
            var totalSucceeded = results.Count(x => x.ClassificationKind == ClassificationKind.Succeeded);

            var runCounts = results
                .Select(x => new ElapsedTimeModel() { JobId = x.JobId, JobName = x.JobName, ElapsedTime = x.DurationSeconds })
                .ToList();

            List<int> runsPerETRange = new List<int>();

            for (int i = 0; i < _ETRangeCount; i++)
            {
                runsPerETRange.Add(0);
            }

            foreach (var runElapsedTime in runCounts)
            {
                int ETDigits = runElapsedTime.ElapsedTime.ToString().Length;
                runsPerETRange[ETDigits - 1] = runsPerETRange[ETDigits - 1] + 1;
            }

            var model = new ElapsedTimeSummaryModel()
            {
                Filter = filter,
                TotalBuildCount = totalCount,
                TotalSucceededCount = totalSucceeded,
                RunCountsPerETRange = runsPerETRange
            };

            return View(viewName: "ElapsedTime", model: model);
        }

        /// <summary>
        /// A view of the total elapsed time per team/project repo, ranked from most elapsed time to least.
        /// </summary>
        /// <returns></returns>
        public ActionResult ProjectElapsedTime(bool pr = false, bool fr = false, DateTimeOffset? startDate = null)
        {
            var filter = CreateBuildFilter(actionName: nameof(ProjectElapsedTime), startDate: startDate, pr: pr, fr: fr, displayFRCheckBox: true);

            List<string> repoNameList = _buildUtil.GetViewNames(filter.StartDate);
            List<ProjectElapsedTimeModel> ETListOfProjects = new List<ProjectElapsedTimeModel>();
            var totalCount = 0;
            var totalSucceeded = 0;
            var totalFlowJobCount = 0;

            foreach (var repoName in repoNameList)
            {
                ProjectElapsedTimeModel currRepo = new ProjectElapsedTimeModel();

                currRepo.RepoName = repoName;

                var results =
                    _buildUtil.GetBuildResults(filter.StartDate, repoName)
                    .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId))
                    .ToList();

                var runCounts = results
                    .Select(x => new ElapsedTimeModel() { JobId = x.JobId, JobName = x.JobName, ElapsedTime = x.DurationSeconds, ClassificationKind = x.ClassificationKind, JobKind = x.JobKind })
                    .ToList();

                foreach (var runElapsedTime in runCounts)
                {
                    //If users choose to exclude flow run results
                    if (!filter.IncludeFlowRunResults)
                    {
                        if (runElapsedTime.JobKind != null && runElapsedTime.JobKind.Equals(JobKind.Flow))
                        {
                            //To avoid double count on total # of flow jobs.
                            if (repoName == AzureUtil.ViewNameAll)
                            {
                                totalFlowJobCount++;
                            }
                            continue;
                        }
                    }

                    currRepo.ETSum = currRepo.ETSum + runElapsedTime.ElapsedTime;

                    if (repoName == AzureUtil.ViewNameAll)
                    {
                        totalCount++;

                        if (runElapsedTime.ClassificationKind == ClassificationKind.Succeeded)
                            totalSucceeded++;
                    }
                }

                //Store total elapsed time in minutes.
                currRepo.ETSum = currRepo.ETSum / 60;

                ETListOfProjects.Add(currRepo);
            }

            var model = new ProjectElapsedTimeSummaryModel()
            {
                Filter = filter,
                TotalBuildCount = totalCount,
                FlowJobCount = totalFlowJobCount,
                TotalSucceededCount = totalSucceeded,
                ProjectElapsedTimeList = ETListOfProjects
            };

            return View(viewName: "ProjectElapsedTime", model: model);
        }

        public ActionResult Kind(string name = null, bool pr = false, DateTime? startDate = null, string viewName = AzureUtil.ViewNameRoslyn)
        {
            var filter = CreateBuildFilter(nameof(Kind), name, viewName, pr, startDate);
            var startDateValue = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            var list = _buildUtil
                .GetBuildResultsByKindName(startDateValue, name, viewName)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobName))
                .ToList();
            var model = new BuildResultKindModel()
            {
                Filter = filter,
                ClassificationKind = name,
                Entries = list,
            };
            return View(viewName: "Kind", model: model);
        }

        public ActionResult KindByViewName(string name = null, bool pr = false, DateTime? startDate = null)
        {
            var filter = CreateBuildFilter(actionName: nameof(KindByViewName), name: name, startDate: startDate, pr: pr);
            var startDateValue = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            var results = _buildUtil
                .GetBuildResultsByKindName(startDateValue, name, AzureUtil.ViewNameAll)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId))
                .ToList();
            var builds = results
                .GroupBy(x => x.ViewName)
                .Select(x => new BuildViewNameModel() { ViewName = x.Key, Count = x.Count() })
                .ToList();
            var model = new BuildResultKindByViewNameModel()
            {
                Filter = filter,
                ClassificationKind = name,
                Builds = builds,
                TotalResultCount = results.Count
            };
            return View(viewName: "KindByViewName", model: model);
        }

        public ActionResult JobListByRepoName(string name = null, bool pr = false, bool fr = false, DateTime? startDate = null, string viewName = AzureUtil.ViewNameAll)
        {
            var startDateValue = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            BuildFilterModel filter;
            List<BuildResultEntity> results;
            var totalJobCount = 0;
            var totalRunCount = 0;
            var totalETOfCurrRepo = 0;
            var totalFlowRunCount = 0;

            //When navigating from "ProjectElapsedTime" view to "JobElapsedTime" view, var "name" is set to the repo name being selected.
            //When refreshing "JobElapsedTime" view via repo name dropdown list, var "viewName" is set to the repo name, var "name" == null
            if (name != null)
            {
                filter = CreateBuildFilter(actionName: nameof(JobListByRepoName), viewName: name, startDate: startDate, pr: pr, fr: fr, displayFRCheckBox: true);
                results = _buildUtil
                    .GetBuildResults(startDateValue, name)
                    .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId))
                    .ToList();
            }
            else
            {
                filter = CreateBuildFilter(actionName: nameof(JobListByRepoName), viewName: viewName, startDate: startDate, pr: pr, fr: fr, displayFRCheckBox: true);
                results = _buildUtil
                    .GetBuildResults(startDateValue, viewName)
                    .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId))
                    .ToList();
            }

            SortedDictionary<string, AgJobElapsedTime> aggregatedJobElapsedTimeDic = new SortedDictionary<string, AgJobElapsedTime>();
            foreach (var entry in results)
            {
                //If users choose to exclude flow run results
                if (!filter.IncludeFlowRunResults)
                {
                    if (entry.JobKind != null && entry.JobKind.Equals(JobKind.Flow))
                    {
                        totalFlowRunCount++;
                        continue;
                    }
                }

                string currJobName = entry.BuildId.JobName;
                totalETOfCurrRepo += entry.DurationSeconds;

                if (aggregatedJobElapsedTimeDic.ContainsKey(currJobName))
                {
                    aggregatedJobElapsedTimeDic[currJobName].ETSum = aggregatedJobElapsedTimeDic[currJobName].ETSum + entry.DurationSeconds;
                    aggregatedJobElapsedTimeDic[currJobName].NumOfBuilds++;
                }
                else
                {
                    AgJobElapsedTime newAgJobElapsedTime = new AgJobElapsedTime();
                    newAgJobElapsedTime.ETSum = entry.DurationSeconds;
                    newAgJobElapsedTime.NumOfBuilds = 1;
                    aggregatedJobElapsedTimeDic.Add(currJobName, newAgJobElapsedTime);
                }
            }

            totalRunCount = results.Count - totalFlowRunCount;
            totalJobCount += aggregatedJobElapsedTimeDic.Count;

            var model = new JobElapsedTimeModel()
            {
                Filter = filter,
                TotalJobCount = totalJobCount,
                TotalRunCount = totalRunCount,
                FlowRunCount = totalFlowRunCount,
                TotalETOfCurrRepo = totalETOfCurrRepo,
                AgJobElapsedTimeDict = aggregatedJobElapsedTimeDic
            };
            return View(viewName: "JobElapsedTime", model: model);
        }

        public ActionResult JobElapsedTimePerBuild(bool pr = false, DateTime? startDate = null, string viewName = AzureUtil.ViewNameRoslyn, string jobName = "dotnet_coreclr/master/checked_windows_nt_bld")
        {
            var startDateValue = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            var filter = CreateBuildFilter(actionName: nameof(JobElapsedTimePerBuild), viewName: viewName, startDate: startDate, pr: pr);
            var results = _buildUtil
                .GetBuildResults(startDateValue, viewName)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId) && x.JobId.Name == jobName)
                .ToList();
            var buildCount = results.Count;
            var totalETOfCurrJob = 0;

            foreach (var entry in results)
            {
                totalETOfCurrJob += entry.DurationSeconds;
            }

            var model = new JobElapsedTimePerBuildModel()
            {
                Filter = filter,
                TotalBuildCount = buildCount,
                TotalETOfCurrJob = totalETOfCurrJob,
                Entries = results
            };

            return View(viewName: "JobElapsedTimePerBuild", model: model);
        }

        public string Csv(string viewName = AzureUtil.ViewNameRoslyn, bool pr = false, DateTime? startDate = null)
        {
            var filter = CreateBuildFilter(nameof(Csv), viewName: viewName, pr: pr, startDate: startDate);
            var summary = GetTestFailureSummaryModel(filter);
            var builder = new StringBuilder();
            foreach (var entry in summary.Entries)
            {
                var name = entry.Name.Replace(',', ' ');
                var index = name.LastIndexOf('.');
                var suiteName = name.Substring(0, index);
                var testName = name.Substring(index + 1);
                builder.AppendLine($"{suiteName},{testName},{entry.Count}");
            }

            return builder.ToString();
        }

        public ActionResult Unprocessed()
        {
            // TODO: Need a simple display for current builds
            //var table = _storage.GetTable(AzureConstants.TableNames.UnprocessedBuild);
            //var list = table.ExecuteQuery(new TableQuery<UnprocessedBuildEntity>()).ToList();
            //return View(viewName: "Unprocessed", model: list);
            throw new Exception();
        }

        private BuildResultSummaryModel GetBuildResultSummaryModel(BuildFilterModel filter)
        {
            var model = new BuildResultSummaryModel()
            {
                Filter = filter,
            };

            var queryResult = _buildUtil
                .GetBuildResults(filter.StartDate, filter.ViewName)
                .Where(x => filter.IncludePullRequests || !JobUtil.IsPullRequestJobName(x.JobId.Name))
                .Where(x => x.ClassificationKind != ClassificationKind.Succeeded)
                .GroupBy(x => x.JobId)
                .Select(x => new { JobId = x.Key, Count = x.Count() })
                .OrderByDescending(x => x.Count)
                .AsEnumerable();

            if (filter.Limit.HasValue)
            {
                queryResult = queryResult.Take(filter.Limit.Value);
            }

            foreach (var entity in queryResult)
            {
                var entry = new BuildResultSummaryEntry()
                {
                    JobId = entity.JobId,
                    Count = entity.Count
                };

                model.Entries.Add(entry);
            }

            return model;
        }

        private BuildResultModel GetBuildResultModel(string jobName, BuildFilterModel filter)
        {
            var model = new BuildResultModel()
            {
                Filter = filter,
                JobId = JobId.ParseName(jobName),
            };

            var queryResult = _buildUtil
                .GetBuildResults(filter.StartDate, jobName, filter.ViewName)
                .Where(x => filter.IncludePullRequests || !JobUtil.IsPullRequestJobName(x.JobId.Name))
                .Where(x => x.ClassificationKind != ClassificationKind.Succeeded)
                .OrderBy(x => x.BuildNumber);

            model.Entries.AddRange(queryResult);
            return model;
        }

        private TestFailureSummaryModel GetTestFailureSummaryModel(BuildFilterModel filter)
        {
            var failureQuery = _buildUtil
                .GetTestCaseFailures(filter.StartDate, filter.ViewName)
                .Where(x => filter.IncludePullRequests || !JobUtil.IsPullRequestJobName(x.BuildId.JobName))
                .GroupBy(x => x.Identifier)
                .Select(x => new { Key = x.Key, Count = x.Count() })
                .OrderByDescending(x => x.Count)
                .AsEnumerable();

            if (filter.Limit.HasValue)
            {
                failureQuery = failureQuery.Take(filter.Limit.Value);
            }

            var summary = new TestFailureSummaryModel()
            {
                Filter = filter,
            };

            foreach (var pair in failureQuery)
            {
                var entry = new TestFailureSummaryEntry()
                {
                    Name = pair.Key,
                    Count = pair.Count
                };
                summary.Entries.Add(entry);
            }

            return summary;
        }

        private TestFailureModel GetTestFailureModel(BuildFilterModel filter)
        {
            var model = new TestFailureModel()
            {
                Filter = filter,
                Name = filter.Name,
            };

            foreach (var entity in _buildUtil.GetTestCaseFailures(filter.StartDate, filter.Name, filter.ViewName))
            {
                var buildId = entity.BuildId;
                if (!filter.IncludePullRequests && JobUtil.IsPullRequestJobName(buildId.JobName))
                {
                    continue;
                }

                model.Builds.Add(entity);
            }

            return model;
        }

        private BuildFilterModel CreateBuildFilter(string actionName, string name = null, string viewName = null, bool pr = false, DateTimeOffset? startDate = null, int? limit = null, bool fr = false, bool displayFRCheckBox = false)
        {
            return new BuildFilterModel()
            {
                Name = name,
                ViewName = viewName,
                IncludePullRequests = pr,
                IncludeFlowRunResults = fr,
                DisplayFlowRunCheckBox = displayFRCheckBox,
                StartDate = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1),
                Limit = limit,
                ActionName = actionName,
            };
        }
    }
}