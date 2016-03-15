using Dashboard.Helpers;
using Dashboard.Models;
using Roslyn.Sql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Dashboard.Controllers
{
    public sealed class TestCache
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
        public string TestRoot { get; set; }
        public string AssemblyName { get; set; }
        public bool IsJenkins { get; set; }
    }

    /// <summary>
    /// This is a proof of concept implementation only.  I realize that its implementation is pretty terrible
    /// and that's fine.  For now it is enough to validate the end to end scenario.
    /// </summary>
    public class TestCacheController : ApiController
    {
        private readonly TestResultStorage _storage;
        private readonly TestCacheStats _stats;

        public TestCacheController()
        {
            _storage = TestResultStorage.Instance;

            var connectionString = ConfigurationManager.AppSettings["jenkins-connection-string"];
            _stats = new TestCacheStats(connectionString);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _stats.Dispose();
            }
        }

        public IEnumerable<string> Get()
        {
            return _storage.Keys;
        }

        public TestResultData Get(string id)
        {
            TestResult testResult;

            if (_storage.TryGetValue(id, out testResult))
            {
                _stats.AddHit(id, assemblyName: null, isJenkins: null);

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

            _stats.AddMiss(id, assemblyName: null, isJenkins: null);
            throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        public void Put(string id, [FromBody]TestCache testCache)
        {
            var testResultData = testCache.TestResultData;
            var seconds = testResultData.ElapsedSeconds > 0
                ? testResultData.ElapsedSeconds
                : testResultData.EllapsedSeconds;
            var testCacheData = new TestResult(
                testResultData.ExitCode,
                testResultData.OutputStandard,
                testResultData.OutputError,
                testResultData.ResultsFileName,
                testResultData.ResultsFileContent,
                TimeSpan.FromSeconds(seconds));

            _storage.Add(id, testCacheData);
            _stats.AddStore(
                id,
                assemblyName: testCache?.TestSourceData?.AssemblyName,
                outputStandardLength: testResultData.OutputStandard?.Length ?? 0,
                outputErrorLength: testResultData.OutputError?.Length ?? 0,
                contentLength: testResultData.ResultsFileContent?.Length ?? 0,
                elapsed: TimeSpan.FromSeconds(testResultData.EllapsedSeconds));
        }
    }
}