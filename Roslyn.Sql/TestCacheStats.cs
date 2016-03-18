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

        public TestCacheStatSummary GetCurrentSummary()
        {
            var tuple = _sqlUtil.GetStats();
            return new TestCacheStatSummary(
                hitCount: tuple.Item1,
                missCount: tuple.Item2,
                storeCount: _sqlUtil.GetStoreCount() ?? 0,
                currentCount: TestResultStorage.Instance.Count,
                runCount: _sqlUtil.GetTestRunCount() ?? 0);
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

        public void AddStore(string checksum, string assemblyName, int outputStandardLength, int outputErrorLength, int contentLength, TimeSpan elapsed)
        {
            _sqlUtil.Insert(checksum, assemblyName, outputStandardLength, outputErrorLength, contentLength, elapsed);
        }

        public bool AddTestRun(TestRun testRun)
        {
            return _sqlUtil.InsertTestRun(testRun);
        }
    }
}