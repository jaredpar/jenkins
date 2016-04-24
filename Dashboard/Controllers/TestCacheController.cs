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

namespace Dashboard.Controllers
{
    public sealed class TestCacheData
    {
        public TestResultData TestResultData { get; set; }
        public TestSourceData TestSourceData { get; set; }
    }

    /// <summary>
    /// The actual test information needs to be cached.
    /// </summary>
    public sealed class TestResultData
    {
        public int ExitCode { get; set; }
        public string OutputStandard { get; set; }
        public string OutputError { get; set; }
        public string ResultsFileName { get; set; }
        public string ResultsFileContent { get; set; }
        public int ElapsedSeconds { get; set; }
        public int TestPassed { get; set; }
        public int TestFailed { get; set; }
        public int TestSkipped { get; set; }

        // Misspelled version to keep until we can flow throw all of the spelling updates.
        public int EllapsedSeconds { get; set; }
    }

    /// <summary>
    /// Extra information about the environment in which the tests were executed.  Helps our
    /// tracking to see how effective the caching is and potentially where errors may be coming
    /// from.
    /// </summary>
    public sealed class TestSourceData
    {
        public string MachineName { get; set; }
        public string EnlistmentRoot { get; set; }
        public string AssemblyName { get; set; }
        public string Source { get; set; }
        public bool IsJenkins { get; set; }
    }

    /// <summary>
    /// This is a proof of concept implementation only.  I realize that its implementation is pretty terrible
    /// and that's fine.  For now it is enough to validate the end to end scenario.
    /// </summary>
    public class TestCacheController : DashboardApiController
    {
        private readonly TestResultStorage _storage;
        private readonly TestCacheStats _stats;
        private readonly CounterStatsUtil _statsUtil;

        public TestCacheController()
        {
            _storage = new TestResultStorage(Storage);
            _stats = new TestCacheStats(_storage);
            _statsUtil = new CounterStatsUtil(Storage);
        }

        public IEnumerable<string> Get()
        {
            return _storage.Keys;
        }

        public TestResultData Get(string id, [FromUri] TestSourceData testSourceData)
        {
            TestResult testResult;

            var buildSource = new BuildSource(testSourceData.MachineName, testSourceData.EnlistmentRoot);
            var isJenkins = string.IsNullOrEmpty(testSourceData.Source)
                ? null
                : (bool?)(testSourceData.Source == "jenkins");

            if (_storage.TryGetValue(id, out testResult))
            {
                _statsUtil.AddHit(isJenkins ?? false);

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
                TimeSpan.FromSeconds(seconds));
            var buildSource = testCacheData.TestSourceData != null
                ? new BuildSource(testCacheData.TestSourceData.MachineName, testCacheData.TestSourceData.EnlistmentRoot)
                : (BuildSource?)null;
            var isJenkins = testCacheData.TestSourceData?.IsJenkins ?? false;

            _storage.Add(id, testResult);
            _statsUtil.AddStore(isJenkins);
            _statsUtil.AddUnitTest(testResultData.TestPassed, testResultData.TestSkipped, testResultData.TestSkipped, TimeSpan.FromSeconds(seconds), isJenkins);
        }
    }
}