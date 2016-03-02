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
    }

    /// <summary>
    /// This is a proof of concept implementation only.  I realize that its implementation is pretty terrible
    /// and that's fine.  For now it is enough to validate the end to end scenario.
    /// </summary>
    public class TestCacheController : ApiController
    {
        private TestResultStorage _storage = TestResultStorage.Instance;
        private TestCacheStats _stats = TestCacheStats.Instance;

        public static SqlUtil CreateConnection()
        {
            var connectionString = ConfigurationManager.AppSettings["jenkins-connection-string"];
            return new SqlUtil(connectionString);
        }

        public IEnumerable<string> Get()
        {
            return _storage.Keys;
        }

        public TestResult Get(string id)
        {
            TestResult testCacheData;
            if (_storage.TryGetValue(id, out testCacheData))
            {
                _stats.AddHit();
                return testCacheData;
            }

            _stats.AddMiss();
            throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        /*
        public void Post(TestCacheData testCacheData)
        {
            Add(testCacheData
        }
        */

        public void Put(string id, [FromBody]TestCache testCache)
        {
            var testResultData = testCache.TestResultData;
            var testCacheData = new TestResult(
                testResultData.ExitCode,
                testResultData.OutputStandard,
                testResultData.OutputError,
                testResultData.ResultsFileName,
                testResultData.ResultsFileContent,
                TimeSpan.FromSeconds(testResultData.EllapsedSeconds));

            _storage.Add(id, testCacheData);
            _stats.AddStore(
                testResultData.OutputStandard?.Length ?? 0,
                testResultData.OutputError?.Length ?? 0,
                testResultData.ResultsFileContent?.Length ?? 0);

            using (var sqlUtil = CreateConnection())
            {
                sqlUtil.Insert(
                    id,
                    assemblyName: testCache.TestSourceData.AssemblyName ?? "",
                    outputStandardLength: testResultData.OutputStandard?.Length ?? 0,
                    outputErrorLength: testResultData.OutputError?.Length ?? 0,
                    contentLength: testResultData.ResultsFileContent?.Length ?? 0);
            }
        }

        // TODO
        public void Delete(int id)
        {

        }

    }
}