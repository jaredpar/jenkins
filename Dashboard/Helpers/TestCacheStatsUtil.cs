using Dashboard.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace Dashboard.Helpers
{
    public sealed class TestCacheStatsUtil
    {
        private static readonly object s_guard = new object();
        private static readonly Guid s_idBase = Guid.NewGuid();
        private static TestCacheCounterEntity s_entity;

        private readonly DashboardStorage _storage;

        public TestCacheStatsUtil(DashboardStorage storage)
        {
            _storage = storage;
        }

        public void AddHit(bool isJenkins)
        {
            var entity = GetEntity(isJenkins);
            entity.HitCount++;
            Update(entity);
        }

        public void AddMiss(bool isJenkins)
        {
            var entity = GetEntity(isJenkins);
            entity.MissCount++;
            Update(entity);
        }

        public void AddStore(bool isJenkins)
        {
            var entity = GetEntity(isJenkins);
            entity.StoreCount++;
            Update(entity);
        }

        private void Update(TestCacheCounterEntity entity)
        {
            var operation = TableOperation.InsertOrReplace(entity);
            _storage.TestCacheCounterTable.Execute(operation);
        }

        private TestCacheCounterEntity GetEntity(bool isJenkins)
        {
            var counterData = new CounterData(GetEntityWriterId(), isJenkins);
            var key = counterData.EntityKey;
            lock (s_guard)
            {
                if (s_entity != null && s_entity.EntityKey == key)
                {
                    return s_entity;
                }
            }

            var entity = GetOrCreateEntity(counterData);
            lock (s_guard)
            {
                s_entity = entity;
            }

            return entity;
        }

        private TestCacheCounterEntity GetOrCreateEntity(CounterData counterData)
        {
            var key = CounterUtil.GetEntityKey(counterData);
            var entity = AzureUtil.QueryTable<TestCacheCounterEntity>(_storage.TestCacheCounterTable, key);
            if (entity != null)
            {
                return entity;
            }

            return new TestCacheCounterEntity(counterData);
        }

        private static string GetEntityWriterId() => $"{s_idBase}-{Thread.CurrentThread.ManagedThreadId}";
    }
}