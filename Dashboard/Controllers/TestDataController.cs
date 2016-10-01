using Dashboard.Azure;
using Dashboard.Azure.TestResults;
using Dashboard.Helpers;
using Dashboard.Helpers.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Net;
using System.Web.Http;

namespace Dashboard.Controllers
{
    [RoutePrefix("api/testData")]
    public class TestDataController : ApiController
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly TestResultStorage _storage;
        private readonly TestCacheStats _stats;
        private readonly CounterStatsUtil _statsUtil;

        public TestDataController()
        {
            _storageAccount = ControllerUtil.CreateStorageAccount();
            _storage = new TestResultStorage(_storageAccount);
            _stats = new TestCacheStats(_storage, _storageAccount.CreateCloudTableClient());
            _statsUtil = new CounterStatsUtil(_storageAccount.CreateCloudTableClient());
        }

        [Route("cache/{id}")]
        [Route("~/api/testCache/{id}")]
        [HttpGet]
        public TestResultData GetTestCache(string id, [FromUri] TestSourceData testSourceData)
        {
            TestResult testResult;

            var buildSource = new BuildSource(testSourceData.MachineName, testSourceData.EnlistmentRoot);
            var isJenkins = string.IsNullOrEmpty(testSourceData.Source)
                ? null
                : (bool?)(testSourceData.Source == "jenkins");

            if (_storage.TryGetValue(id, out testResult))
            {
                var isJenkinsValue = isJenkins ?? false;
                _statsUtil.AddHit(isJenkinsValue);
                _statsUtil.AddUnitTestQuery(testResult.UnitTestData, testResult.Elapsed, isJenkinsValue);

                var testResultData = new TestResultData()
                {
                    ExitCode = testResult.ExitCode,
                    OutputStandard = testResult.OutputStandard,
                    OutputError = testResult.OutputError,
                    ResultsFileName = testResult.ResultsFileName,
                    ResultsFileContent = testResult.ResultsFileContent,
                    ElapsedSeconds = (int)testResult.Elapsed.TotalSeconds
                };
                return testResultData;
            }

            _statsUtil.AddMiss(isJenkins ?? false);
            throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        [Route("cache/{id}")]
        [Route("~/api/testCache/{id}")]
        [HttpPut]
        public void PutTestCache(string id, [FromBody] TestCacheData testCacheData)
        {
            var testResultData = testCacheData.TestResultData;
            var seconds = testResultData.ElapsedSeconds;
            var testResult = new TestResult(
                testResultData.ExitCode,
                testResultData.OutputStandard,
                testResultData.OutputError,
                testResultData.ResultsFileName,
                testResultData.ResultsFileContent,
                new UnitTestData(
                    passed: testResultData.TestPassed,
                    failed: testResultData.TestFailed,
                    skipped: testResultData.TestSkipped),
                TimeSpan.FromSeconds(seconds));
            var buildSource = testCacheData.TestSourceData != null
                ? new BuildSource(testCacheData.TestSourceData.MachineName, testCacheData.TestSourceData.EnlistmentRoot)
                : (BuildSource?)null;
            var isJenkins = testCacheData.TestSourceData?.IsJenkins ?? false;

            _storage.Add(id, testResult);
            _statsUtil.AddStore(isJenkins);
        }

        [Route("~/api/testRun")]
        [Route("run")]
        [HttpPut]
        [HttpPost]
        public void Post([FromBody] TestRunData testRunData)
        {
            var elapsed = testRunData.ElapsedSeconds;

            // TODO: Need to send along build source from the test runner.
            var buildSource = BuildSource.CreateAnonymous();
            var runDate = DateTime.UtcNow;
            var entity = new TestRunEntity(runDate, buildSource)
            {
                CacheType = testRunData.Cache,
                ElapsedSeconds = elapsed,
                Succeeded = testRunData.Succeeded,
                IsJenkins = testRunData.IsJenkins,
                Is32Bit = testRunData.Is32Bit,
                CacheCount = testRunData.CacheCount,
                ChunkCount = testRunData.ChunkCount,
                AssemblyCount = testRunData.AssemblyCount,
                JenkinsUrl = testRunData.JenkinsUrl,
                HasErrors = testRunData.HasErrors,
            };

            var testRunTable = _storageAccount.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.TestRunData);
            _statsUtil.AddTestRun(entity.Succeeded, entity.IsJenkins);
            var operation = TableOperation.Insert(entity);
            testRunTable.Execute(operation);
        }
    }
}