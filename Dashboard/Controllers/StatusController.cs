using Dashboard.Helpers;
using Dashboard.Models;
using Dashboard;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Microsoft.WindowsAzure;
using Dashboard.Azure;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage.Table;
using Dashboard.Azure.TestResults;

namespace Dashboard.Controllers
{
    public class StatusController : Controller
    {
        private readonly TestCacheStats _testCacheStats;
        private readonly TestResultStorage _testResultStorage;
        private readonly CloudTable _testRunTable;

        public StatusController()
        {
            var storage = ControllerUtil.CreateDashboardStorage();
            _testResultStorage = new TestResultStorage(storage);
            _testCacheStats = new TestCacheStats(_testResultStorage);
            _testRunTable = storage.StorageAccount.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.TestRunData);
        }

        public ActionResult Index()
        {
            return RedirectToAction(nameof(Tests));
        }

        public ActionResult Tests([FromUri] bool all = false)
        {
            var startDate = all
                ? (DateTime?)null
                : DateTime.UtcNow - TimeSpan.FromDays(1);
            return View(_testCacheStats.GetSummary(startDate));
        }

        public ActionResult Result(string id)
        {
            TestResult testResult;
            if (!_testResultStorage.TryGetValue(id, out testResult))
            {
                throw new Exception("Invalid key");
            }

            var contentType = testResult.ResultsFileName.EndsWith("xml")
                ? "application/xml"
                : "text/html";
            return Content(testResult.ResultsFileContent, contentType);
        }

        public ActionResult Results()
        {
            return View(_testResultStorage.Keys);
        }

        public ActionResult Errors()
        {
            return View("");
        }

        public ActionResult TestRuns([FromUri] string startDate = null, [FromUri] string endDate = null)
        {
            var startDateTime = ParameterToDateTime(startDate);
            var endDateTime = ParameterToDateTime(endDate);

            var testRunList = GetTestRuns(
                startDateTime ?? DateTime.UtcNow - TimeSpan.FromDays(7),
                endDateTime ?? DateTime.UtcNow).ToList();
            var compList = GetTestRunComparisons(testRunList);
            return View(compList);
        }

        // TODO: this is a full table scan.  Terrible.  Should be optimized.
        private IEnumerable<TestRunEntity> GetTestRuns(DateTime startDate, DateTime endDate)
        {
            Debug.Assert(startDate.Kind == DateTimeKind.Utc);
            Debug.Assert(endDate.Kind == DateTimeKind.Utc);

            var startFilter = TableQuery.GenerateFilterConditionForDate(
                nameof(TestRunEntity.RunDate),
                QueryComparisons.GreaterThanOrEqual,
                startDate);
            var endFilter = TableQuery.GenerateFilterConditionForDate(
                nameof(TestRunEntity.RunDate),
                QueryComparisons.LessThanOrEqual,
                endDate);
            var filter = TableQuery.CombineFilters(
                startFilter,
                TableOperators.And,
                endFilter);

            var query = new TableQuery<TestRunEntity>().Where(filter);
            return _testRunTable.ExecuteQuery(query);
        }

        private static Dictionary<DateTime, List<TestRunEntity>> GetTestRunGroupedByDate(List<TestRunEntity> testRunList)
        {
            // First group the data by date
            var map = new Dictionary<DateTime, List<TestRunEntity>>();
            foreach (var cur in testRunList)
            {
                if (!cur.Succeeded || !cur.IsJenkins || cur.CacheType == "test" || cur.AssemblyCount < 35 || cur.Elapsed.Ticks == 0)
                {
                    continue;
                }

                var date = cur.RunDate.Date;
                List<TestRunEntity> list;
                if (!map.TryGetValue(date, out list))
                {
                    list = new List<TestRunEntity>();
                    map[date] = list;
                }

                list.Add(cur);
            }

            return map;
        }

        /// <summary>
        /// Break up the test runs into a list of comparisons by the date on which they occured. 
        /// </summary>
        private static List<TestRunComparison> GetTestRunComparisons(List<TestRunEntity> testRunList)
        {
            var map = GetTestRunGroupedByDate(testRunList);
            var compList = new List<TestRunComparison>(map.Count);
            var cachedList = new List<TimeSpan>();
            var noCachedList = new List<TimeSpan>();
            var allList = new List<TimeSpan>();
            foreach (var pair in map.OrderBy(x => x.Key))
            {
                cachedList.Clear();
                noCachedList.Clear();
                allList.Clear();

                var countHighCached = 0;
                foreach (var item in pair.Value)
                {
                    allList.Add(item.Elapsed);

                    if (item.CacheCount > 0)
                    {
                        cachedList.Add(item.Elapsed);
                        if (((double)item.CacheCount / item.ChunkCount) > .5)
                        {
                            countHighCached++;
                        }
                    }
                    else
                    {
                        noCachedList.Add(item.Elapsed);
                    }
                }

                var comp = new TestRunComparison()
                {
                    Date = pair.Key,
                    AverageTimeCached = Average(cachedList),
                    AverageTimeNoCached = Average(noCachedList),
                    AverageTimeAll = Average(allList),
                    Count = allList.Count,
                    CountHighCached = countHighCached,
                    CountCached = cachedList.Count,
                    CountNoCached = noCachedList.Count,
                };

                if (noCachedList.Count == 0)
                {
                    comp.TimeSaved = TimeSpan.FromSeconds(0);
                }
                else
                {
                    var timeSavedSeconds =
                        (comp.AverageTimeNoCached.TotalSeconds * comp.Count) -
                        (pair.Value.Sum(x => x.Elapsed.TotalSeconds));
                    comp.TimeSaved = TimeSpan.FromSeconds(timeSavedSeconds);
                }

                compList.Add(comp);
            }

            return compList;
        }

        private static TimeSpan Average(IEnumerable<TimeSpan> e)
        {
            if (!e.Any())
            {
                return TimeSpan.FromSeconds(0);
            }

            var average = e.Average(x => x.Ticks);
            return TimeSpan.FromTicks((long)average);
        }

        private static DateTime? ParameterToDateTime(string p, DateTime? defaultValue = null)
        {
            if (string.IsNullOrEmpty(p))
            {
                return defaultValue;
            }

            DateTime dateTime;
            if (DateTime.TryParse(p, out dateTime))
            {
                return dateTime;
            }

            return defaultValue;
        }
    }
}