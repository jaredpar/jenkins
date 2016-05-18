using Dashboard.Helpers;
using Dashboard.Models;
using Dashboard;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.WindowsAzure;
using Dashboard.Azure;
using Dashboard.Helpers.Json;

namespace Dashboard.Controllers
{
    /// <summary>
    /// This is a proof of concept implementation only.  I realize that its implementation is pretty terrible
    /// and that's fine.  For now it is enough to validate the end to end scenario.
    /// </summary>
    public class TestCacheController : ApiController
    {
        private readonly TestResultStorage _storage;
        private readonly TestCacheStats _stats;
        private readonly CounterStatsUtil _statsUtil;

        public TestCacheController()
        {
            var storage = ControllerUtil.CreateDashboardStorage();
            _storage = new TestResultStorage(storage);
            _stats = new TestCacheStats(_storage);
            _statsUtil = new CounterStatsUtil(storage);
        }

        public IEnumerable<string> Get()
        {
            return _storage.Keys;
        }

        public TestResultData Get(string id, [FromUri] TestSourceData testSourceData)
        {
            Azure.TestResult testResult;

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

        public void Put(string id, [FromBody] TestCacheData testCacheData)
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
    }
}