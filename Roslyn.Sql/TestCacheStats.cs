using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Roslyn.Sql
{
    public sealed class TestCacheStats : IDisposable
    {
        private readonly SqlUtil _sqlUtil;

        public TestCacheStats(string connectionString = null)
        {
            _sqlUtil = new SqlUtil(connectionString);
        }

        public void Dispose()
        {
            _sqlUtil.Dispose();
        }

        public TestCacheStatSummary GetSummary(DateTime? startDate)
        {
            return new TestCacheStatSummary(
                hitStats: _sqlUtil.GetHitStats(startDate) ?? default(TestHitStats),
                missCount: _sqlUtil.GetMissStats(startDate) ?? 0,
                storeCount: _sqlUtil.GetStoreCount(startDate) ?? 0,
                cacheCount: _sqlUtil.GetTestResultCount(startDate) ?? 0,
                runCount: _sqlUtil.GetTestRunCount(startDate) ?? 0);
        }

        public void AddHit(string checksum, string assemblyName, bool? isJenkins, BuildSource? buildSource)
        {
            var buildSourceId = _sqlUtil.GetBuildSourceId(buildSource?.MachineName, buildSource?.EnlistmentRoot);
            _sqlUtil.InsertHit(checksum, assemblyName, isJenkins, buildSourceId);
        }

        public void AddMiss(string checksum, string assemblyName, bool? isJenkins, BuildSource? buildSource)
        {
            var buildSourceId = _sqlUtil.GetBuildSourceId(buildSource?.MachineName, buildSource?.EnlistmentRoot);
            _sqlUtil.InsertMiss(checksum, assemblyName, isJenkins, buildSourceId);
        }

        public void AddStore(string checksum, string assemblyName, int outputStandardLength, int outputErrorLength, int contentLength, TestResultSummary summary, BuildSource? buildSource)
        {
            var buildSourceId = _sqlUtil.GetBuildSourceId(buildSource?.MachineName, buildSource?.EnlistmentRoot);
            _sqlUtil.Insert(checksum, assemblyName, outputStandardLength, outputErrorLength, contentLength, summary, buildSourceId);
        }

        public bool AddTestRun(TestRun testRun)
        {
            return _sqlUtil.InsertTestRun(testRun);
        }
    }
}