using Dashboard.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Sql
{
    public sealed class TestCacheStats
    {
        private readonly SqlUtil _sqlUtil;
        private readonly TestResultStorage _testResultStorage;

        public TestCacheStats(TestResultStorage testResultStorage, SqlUtil sqlUtil)
        {
            _testResultStorage = testResultStorage;
            _sqlUtil = sqlUtil;
        }

        public TestCacheStatSummary GetSummary(DateTime? startDate)
        {
            return new TestCacheStatSummary(
                hitStats: _sqlUtil.GetHitStats(startDate) ?? default(TestHitStats),
                missCount: _sqlUtil.GetMissStats(startDate) ?? 0,
                uploadCount: _sqlUtil.GetStoreCount(startDate) ?? 0,
                testResultCount: _testResultStorage.GetCount(startDate));
        }

        public List<TestRun> GetTestRuns(DateTime? startDate = null, DateTime? endDate = null)
        {
            return _sqlUtil.GetTestRuns(startDate, endDate);
        }
    }
}