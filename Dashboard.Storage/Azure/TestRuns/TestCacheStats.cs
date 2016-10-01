using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Dashboard.Azure.TestRuns
{
    public sealed class TestCacheStats
    {
        private readonly TestResultStorage _testResultStorage;
        private readonly CounterUtil<UnitTestCounterEntity> _unitTestCounterUtil;
        private readonly CounterUtil<TestCacheCounterEntity> _testCacheCounterUtil;
        private readonly CounterUtil<TestRunCounterEntity> _testRunCounterUtil;

        public TestCacheStats(TestResultStorage testResultStorage, CloudTableClient tableClient)
        {
            _testResultStorage = testResultStorage;
            _unitTestCounterUtil = new CounterUtil<UnitTestCounterEntity>(tableClient.GetTableReference(AzureConstants.TableNames.CounterUnitTestQuery));
            _testCacheCounterUtil = new CounterUtil<TestCacheCounterEntity>(tableClient.GetTableReference(AzureConstants.TableNames.CounterTestCache));
            _testRunCounterUtil = new CounterUtil<TestRunCounterEntity>(tableClient.GetTableReference(AzureConstants.TableNames.CounterTestRun));
        }

        public TestCacheStatSummary GetSummary(DateTimeOffset? startDate)
        {
            var startDateValue = startDate ?? AzureUtil.DefaultStartDate;
            var endDateValue = DateTimeOffset.UtcNow.Date;

            var stats = new TestQueryStats();
            var unitTestQuery = _unitTestCounterUtil.Query(startDateValue, endDateValue);
            foreach (var cur in unitTestQuery)
            {
                stats.AssemblyCount += cur.AssemblyCount;
                stats.TestsPassed += cur.TestsPassed;
                stats.TestsSkipped += cur.TestsSkipped;
                stats.TestsFailed += cur.TestsFailed;
                stats.ElapsedSeconds += cur.ElapsedSeconds;
            }

            var missCount = 0;
            var uploadCount = 0;
            var cacheQuery = _testCacheCounterUtil.Query(startDateValue, endDateValue);
            foreach (var cur in cacheQuery)
            {
                missCount += cur.MissCount;
                uploadCount += cur.StoreCount;
            }

            var testRunCount = 0;
            var testRunQuery = _testRunCounterUtil.Query(startDateValue, endDateValue);
            foreach (var cur in testRunQuery)
            {
                testRunCount += cur.RunCount;
            }

            return new TestCacheStatSummary(
                hitStats: stats,
                missCount: missCount,
                uploadCount: uploadCount,
                testResultCount: _testResultStorage.GetCount(startDate),
                testRunCount: testRunCount);
        }
    }
}