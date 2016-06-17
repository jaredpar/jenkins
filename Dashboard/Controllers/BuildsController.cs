using Dashboard.Models;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Dashboard;
using Dashboard.Azure;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Dashboard.Helpers;
using System.Text;

namespace Dashboard.Controllers
{
    public class BuildsController : Controller
    {
        private readonly DashboardStorage _storage;
        private readonly BuildUtil _buildUtil;

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

            for (int i = 0; i < 6; i++)
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
        public ActionResult RepoET(bool pr = false, DateTimeOffset? startDate = null)
        {
            var filter = CreateBuildFilter(actionName: nameof(RepoET), startDate: startDate, pr: pr);

            List<string> repoNameList = _buildUtil.GetViewNames(filter.StartDate);
            List<RepoETModel> ETListOfRepos = new List<RepoETModel>();
            var totalCount = 0;
            var totalSucceeded = 0;

            foreach (var repoName in repoNameList)
            {
                RepoETModel currRepo = new RepoETModel();

                currRepo.RepoName = repoName;

                var results =
                _buildUtil.GetBuildResults(filter.StartDate, repoName)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId))
                .ToList();

                if (repoName == "all")
                {
                    totalCount = results.Count;
                    totalSucceeded = results.Count(x => x.ClassificationKind == ClassificationKind.Succeeded);
                }

                var runCounts = results
                    .Select(x => new ElapsedTimeModel() { JobId = x.JobId, JobName = x.JobName, ElapsedTime = x.DurationSeconds })
                    .ToList();

                foreach (var runElapsedTime in runCounts)
                {
                    currRepo.ETSum = currRepo.ETSum + runElapsedTime.ElapsedTime;
                }

                //Store total elapsed time in minutes.
                currRepo.ETSum = currRepo.ETSum / 60;

                ETListOfRepos.Add(currRepo);
            }

            var model = new RepoETSummaryModel()
            {
                Filter = filter,
                TotalBuildCount = totalCount,
                TotalSucceededCount = totalSucceeded,
                RepoETList = ETListOfRepos
            };

            return View(viewName: "RepoET", model: model);
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

        public ActionResult JobListByRepoName(string name = null, bool pr = false, DateTime? startDate = null)
        {
            var filter = CreateBuildFilter(actionName: nameof(JobListByRepoName), viewName: name, startDate: startDate, pr: pr);
            var startDateValue = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            var results = _buildUtil
                .GetBuildResults(startDateValue, name)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId))
                .ToList();

            SortedDictionary< string, AgJobET> aggregatedJobETDic = new SortedDictionary<string, AgJobET>();
            foreach (var entry in results)
            {
                string currJobName = entry.BuildId.JobName;
                if (aggregatedJobETDic.ContainsKey(currJobName))
                {
                    aggregatedJobETDic[currJobName].ETSum = aggregatedJobETDic[currJobName].ETSum + entry.DurationSeconds;
                    aggregatedJobETDic[currJobName].NumOfBuilds++;
                }
                else
                {
                    AgJobET newAgJobET = new AgJobET();
                    newAgJobET.ETSum = entry.DurationSeconds;
                    newAgJobET.NumOfBuilds = 1;
                    aggregatedJobETDic.Add(currJobName, newAgJobET);
                }
            }

            var model = new JobETModel()
            {
                Filter = filter,
                AgJobETDict = aggregatedJobETDic
            };
            return View(viewName: "JobET", model: model);
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
            var table = _storage.GetTable(AzureConstants.TableNames.UnprocessedBuild);
            var list = table.ExecuteQuery(new TableQuery<UnprocessedBuildEntity>()).ToList();
            return View(viewName: "Unprocessed", model: list);
        }

        public ActionResult Demand(string userName, string commit)
        {
            var runStatus = new DemandRunStatusModel()
            {
                UserName = userName,
                Commit = commit,
            };

            var query = new TableQuery<DemandBuildEntity>()
                .Where(DashboardStorage.GenerateDemandBuildFilter(userName, commit));
            foreach (var entity in _storage.DemandBuildTable.ExecuteQuery(query))
            {
                var status = new DemandBuildStatusModel()
                {
                    BuildNumber = entity.BuildNumber,
                    JobName = entity.JobName,
                    QueueNumber = entity.QueueItemNumber
                };
                runStatus.StatusList.Add(status);
            }

            return View(viewName: "DemandStatus", model: runStatus);
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

        private BuildFilterModel CreateBuildFilter(string actionName, string name = null, string viewName = null, bool pr = false, DateTimeOffset? startDate = null, int? limit = null)
        {
            return new BuildFilterModel()
            {
                Name = name,
                ViewName = viewName,
                IncludePullRequests = pr,
                StartDate = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1),
                Limit = limit,
                ActionName = actionName,
            };
        }
    }
}