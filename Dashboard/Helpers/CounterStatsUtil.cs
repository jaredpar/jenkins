using Dashboard.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace Dashboard.Helpers
{
    public sealed class CounterStatsUtil
    {
        private readonly CounterEntityUtil<TestCacheCounterEntity> _testCacheEntityUtil;
        private readonly CounterEntityUtil<UnitTestCounterEntity> _unitTestEntityUtil;
        private readonly CounterEntityUtil<TestRunCounterEntity> _testRunEntityUtil;

        public CounterStatsUtil(DashboardStorage storage)
        {
            var client = storage.StorageAccount.CreateCloudTableClient();
            _testCacheEntityUtil = new CounterEntityUtil<TestCacheCounterEntity>(
                client.GetTableReference(AzureConstants.TableNames.TestCacheCounter),
                x => new TestCacheCounterEntity(x));
            _unitTestEntityUtil = new CounterEntityUtil<UnitTestCounterEntity>(
                client.GetTableReference(AzureConstants.TableNames.UnitTestQueryCounter),
                x => new UnitTestCounterEntity(x));
            _testRunEntityUtil = new CounterEntityUtil<TestRunCounterEntity>(
                client.GetTableReference(AzureConstants.TableNames.TestRunCounter),
                x => new TestRunCounterEntity(x));
        }

        public void AddHit(bool isJenkins)
        {
            var entity = _testCacheEntityUtil.GetEntity(isJenkins);
            entity.HitCount++;
            _testCacheEntityUtil.Update(entity);
        }

        public void AddMiss(bool isJenkins)
        {
            var entity = _testCacheEntityUtil.GetEntity(isJenkins);
            entity.MissCount++;
            _testCacheEntityUtil.Update(entity);
        }

        public void AddStore(bool isJenkins)
        {
            var entity = _testCacheEntityUtil.GetEntity(isJenkins);
            entity.StoreCount++;
            _testCacheEntityUtil.Update(entity);
        }

        public void AddUnitTestQuery(UnitTestData unitTestData, TimeSpan elapsed, bool isJenkins)
        {
            var entity = _unitTestEntityUtil.GetEntity(isJenkins);
            entity.AssemblyCount++;
            entity.TestsPassed += unitTestData.Passed;
            entity.TestsFailed += unitTestData.Failed;
            entity.TestsSkipped += unitTestData.Skipped;
            entity.ElapsedSeconds += (long)elapsed.TotalSeconds;
            _unitTestEntityUtil.Update(entity);
        }

        public void AddTestRun(bool succeeded, bool isJenkins)
        {
            var entity = _testRunEntityUtil.GetEntity(isJenkins);
            entity.RunCount++;
            if (succeeded)
            {
                entity.SucceededCount++;
            }
            _testRunEntityUtil.Update(entity);
        }
    }
}