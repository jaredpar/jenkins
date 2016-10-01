using Dashboard.Azure;
using Dashboard.Azure.TestRuns;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using static Dashboard.Azure.AzureConstants;

namespace Dashboard.Helpers
{
    public sealed class CounterStatsUtil
    {
        private readonly CounterUtil<TestCacheCounterEntity> _testCacheEntityUtil;
        private readonly CounterUtil<TestCacheCounterEntity> _testCacheEntityUtilJenkins;
        private readonly CounterUtil<UnitTestCounterEntity> _unitTestEntityUtil;
        private readonly CounterUtil<UnitTestCounterEntity> _unitTestEntityUtilJenkins;
        private readonly CounterUtil<TestRunCounterEntity> _testRunEntityUtil;
        private readonly CounterUtil<TestRunCounterEntity> _testRunEntityUtilJenkins;

        public CounterStatsUtil(CloudTableClient client)
        {
            _testCacheEntityUtil = new CounterUtil<TestCacheCounterEntity>(client.GetTableReference(TableNames.CounterTestCache));
            _testCacheEntityUtilJenkins = new CounterUtil<TestCacheCounterEntity>(client.GetTableReference(TableNames.CounterTestCacheJenkins));
            _unitTestEntityUtil = new CounterUtil<UnitTestCounterEntity>(client.GetTableReference(TableNames.CounterUnitTestQuery));
            _unitTestEntityUtilJenkins = new CounterUtil<UnitTestCounterEntity>(client.GetTableReference(TableNames.CounterUnitTestQueryJenkins));
            _testRunEntityUtil = new CounterUtil<TestRunCounterEntity>(client.GetTableReference(TableNames.CounterTestRun));
            _testRunEntityUtilJenkins = new CounterUtil<TestRunCounterEntity>(client.GetTableReference(TableNames.CounterTestRunJenkins));
        }

        public void AddHit(bool isJenkins)
        {
            AddHit(_testCacheEntityUtil);
            if (isJenkins)
            {
                AddHit(_testCacheEntityUtilJenkins);
            }
        }

        private static void AddHit(CounterUtil<TestCacheCounterEntity> util)
        {
            var entity = util.GetEntity();
            entity.HitCount++;
            util.Update(entity);
        }

        public void AddMiss(bool isJenkins)
        {
            AddMiss(_testCacheEntityUtil);
            if (isJenkins)
            {
                AddHit(_testCacheEntityUtilJenkins);
            }
        }

        private static void AddMiss(CounterUtil<TestCacheCounterEntity> util)
        { 
            var entity = util.GetEntity();
            entity.MissCount++;
            util.Update(entity);
        }

        public void AddStore(bool isJenkins)
        {
            AddStore(_testCacheEntityUtil);
            if (isJenkins)
            {
                AddStore(_testCacheEntityUtilJenkins);
            }
        }

        private static void AddStore(CounterUtil<TestCacheCounterEntity> util)
        { 
            var entity = util.GetEntity();
            entity.StoreCount++;
            util.Update(entity);
        }

        public void AddUnitTestQuery(UnitTestData unitTestData, TimeSpan elapsed, bool isJenkins)
        {
            AddUnitTestQuery(_unitTestEntityUtil, unitTestData, elapsed);
            if (isJenkins)
            {
                AddUnitTestQuery(_unitTestEntityUtilJenkins, unitTestData, elapsed);
            }
        }

        private static void AddUnitTestQuery(CounterUtil<UnitTestCounterEntity> util, UnitTestData unitTestData, TimeSpan elapsed)
        { 
            var entity = util.GetEntity();
            entity.AssemblyCount++;
            entity.TestsPassed += unitTestData.Passed;
            entity.TestsFailed += unitTestData.Failed;
            entity.TestsSkipped += unitTestData.Skipped;
            entity.ElapsedSeconds += (long)elapsed.TotalSeconds;
            util.Update(entity);
        }

        public void AddTestRun(bool succeeded, bool isJenkins)
        {
            AddTestRun(_testRunEntityUtil, succeeded);
            if (isJenkins)
            {
                AddTestRun(_testRunEntityUtilJenkins, succeeded);
            }
        }

        private static void AddTestRun(CounterUtil<TestRunCounterEntity> util, bool succeeded)
        { 
            var entity = util.GetEntity();
            entity.RunCount++;
            if (succeeded)
            {
                entity.SucceededCount++;
            }
            util.Update(entity);
        }
    }
}