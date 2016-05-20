using Dashboard.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Dashboard.Azure
{
    public sealed class TestCacheStats
    {
        private readonly DashboardStorage _storage;
        private readonly TestResultStorage _testResultStorage;
        private readonly CloudTable _unitTestCounterTable;
        private readonly CloudTable _testCacheCounterTable;
        private readonly CloudTable _testRunCounterTable;

        public TestCacheStats(TestResultStorage testResultStorage)
        {
            _testResultStorage = testResultStorage;
            _storage = _testResultStorage.DashboardStorage;

            var tableClient = _storage.StorageAccount.CreateCloudTableClient();
            _unitTestCounterTable = tableClient.GetTableReference(AzureConstants.TableNames.UnitTestQueryCounter);
            _testCacheCounterTable = tableClient.GetTableReference(AzureConstants.TableNames.TestCacheCounter);
            _testRunCounterTable = tableClient.GetTableReference(AzureConstants.TableNames.TestRunCounter);
        }

        public TestCacheStatSummary GetSummary(DateTimeOffset? startDate)
        {
            var startDateValue = startDate ?? AzureUtil.DefaultStartDate;
            var endDateValue = DateTimeOffset.UtcNow;

            var stats = new TestQueryStats();
            var unitTestQuery = CounterUtil.Query<UnitTestCounterEntity>(_unitTestCounterTable, startDateValue, endDateValue);
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
            var cacheQuery = CounterUtil.Query<TestCacheCounterEntity>(_testCacheCounterTable, startDateValue, endDateValue);
            foreach (var cur in cacheQuery)
            {
                missCount += cur.MissCount;
                uploadCount += cur.StoreCount;
            }

            var testRunCount = 0;
            var testRunQuery = CounterUtil.Query<TestRunCounterEntity>(_testRunCounterTable, startDateValue, endDateValue);
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