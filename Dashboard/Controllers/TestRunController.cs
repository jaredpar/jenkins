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
    public sealed class TestRunData
    {
        public string Cache { get; set;}
        public int EllapsedSeconds { get; set;}
        public bool Succeeded { get; set; }
        public bool IsJenkins { get; set;}
        public bool Is32Bit { get; set;}
        public int AssemblyCount { get; set;}
        public int CacheCount { get; set;}
    }

    public class TestRunController : ApiController
    {
        private readonly TestCacheStats _stats;

        public TestRunController()
        {
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

        public void Post([FromBody] TestRunData testRunData)
        {
        var testRun = new TestRun(
                runDate: DateTime.UtcNow,
                cache: testRunData.Cache,
                ellapsed: TimeSpan.FromSeconds(testRunData.EllapsedSeconds),
                succeeded: testRunData.Succeeded,
                isJenkins: testRunData.IsJenkins,
                is32Bit: testRunData.Is32Bit,
                cacheCount: testRunData.CacheCount,
                assemblyCount: testRunData.AssemblyCount);
            if (!_stats.AddTestRun(testRun))
            {
                throw new Exception("Unable to insert data");
            }
        }
    }
}