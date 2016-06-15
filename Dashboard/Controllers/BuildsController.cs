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
        public ActionResult ElapsedTime(bool pr = false, bool succeeded = false, DateTimeOffset? startDate = null, string viewName = AzureUtil.ViewNameRoslyn)
        {
            var startDateValue = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            var results =
                _buildUtil.GetBuildResults(startDateValue, viewName)
                .Where(x => pr || !JobUtil.IsPullRequestJobName(x.JobId))
                .ToList();

            var totalCount = results.Count;
            var totalSucceeded = results.Count(x => x.ClassificationKind == ClassificationKind.Succeeded);

            var builds = results
                .Where(x => succeeded || x.ClassificationKind != ClassificationKind.Succeeded)
                .GroupBy(x => x.ClassificationName)
                .Select(x => new BuildViewModel() { KindName = x.Key, Count = x.Count() })
                .ToList();

            var model = new ElapsedTimeSummaryModel()
            {
                IncludePullRequests = pr,
                IncludeSucceeded = succeeded,
                TotalBuildCount = totalCount,
                TotalSucceededCount = totalSucceeded,
                StartDate = startDateValue,
                Builds = builds,
                SelectedViewName = viewName
            };

            return View(viewName: "ElapsedTime", model: model);
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