using Dashboard.Helpers;
using Dashboard.Models;
using Dashboard;
using Dashboard.Sql;
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
    public sealed class TestRunData
    {
        public string Cache { get; set; }
        public int ElapsedSeconds { get; set; }
        public bool Succeeded { get; set; }
        public bool IsJenkins { get; set; }
        public bool Is32Bit { get; set; }
        public int AssemblyCount { get; set; }
        public int CacheCount { get; set; }
        public int ChunkCount { get; set; }

        // Misspelled version to keep until we can flow throw all of the spelling updates.
        public int EllapsedSeconds { get; set; }
    }

    public class TestRunController : ApiController
    {
        private readonly SqlUtil _sqlUtil;
        private readonly TestCacheStats _stats;
        private readonly TestCacheStatsUtil _statsUtil;
        private readonly TestResultStorage _storage;

        public TestRunController()
        {
            var connectionString = ConfigurationManager.AppSettings[SharedConstants.SqlConnectionStringName];
            _sqlUtil = new SqlUtil(connectionString);

            var dashboardConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            var dashboardStorage = new DashboardStorage(dashboardConnectionString);
            var storage = new TestResultStorage(dashboardStorage);

            _stats = new TestCacheStats(storage, _sqlUtil);
            _statsUtil = new TestCacheStatsUtil(dashboardStorage);
            _storage = new TestResultStorage(dashboardStorage);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _sqlUtil.Dispose();
            }
        }

        public void Post([FromBody] TestRunData testRunData)
        {
            var elapsed = testRunData.ElapsedSeconds > 0
                ? testRunData.ElapsedSeconds
                : testRunData.EllapsedSeconds;

            var testRun = new TestRun(
                    runDate: DateTime.UtcNow,
                    cache: testRunData.Cache,
                    elapsed: TimeSpan.FromSeconds(elapsed),
                    succeeded: testRunData.Succeeded,
                    isJenkins: testRunData.IsJenkins,
                    is32Bit: testRunData.Is32Bit,
                    cacheCount: testRunData.CacheCount,
                    chunkCount: testRunData.ChunkCount,
                    assemblyCount: testRunData.AssemblyCount);

            // TODO: Need to store this data somewhere
            // _statsUtil.AddRun();
        }
    }
}