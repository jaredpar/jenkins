using Dashboard.Azure;
using Dashboard.Helpers;
using Dashboard.Helpers.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;

namespace Dashboard.Controllers
{
    [RoutePrefix("api/testData")]
    public class TestDataController : ApiController
    {
        private readonly TestResultStorage _storage;
        private readonly TestCacheStats _stats;
        private readonly CounterStatsUtil _statsUtil;

        public TestDataController()
        {
            var dashboardStorage = ControllerUtil.CreateDashboardStorage();
            _storage = new TestResultStorage(dashboardStorage);
            _stats = new TestCacheStats(_storage);
            _statsUtil = new CounterStatsUtil(dashboardStorage);
        }

        [Route("cache/{id}")]
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
                    ElapsedSeconds = (int)testResult.Elapsed.TotalSeconds,
                    EllapsedSeconds = (int)testResult.Elapsed.TotalSeconds,
                };
                return testResultData;
            }

            _statsUtil.AddMiss(isJenkins ?? false);
            throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        [Route("cache/{id}")]
        [HttpPut]
        public void PutTestCache(string id, [FromBody] TestCacheData testCacheData)
        {
            var testResultData = testCacheData.TestResultData;
            var seconds = testResultData.ElapsedSeconds > 0
                ? testResultData.ElapsedSeconds
                : testResultData.EllapsedSeconds;
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

        [Route("run")]
        [HttpGet]
        public string RunTest()
        {
            return "run";
        }

        [Route("run")]
        public void PutTest(string data)
        {

        }
    }
}