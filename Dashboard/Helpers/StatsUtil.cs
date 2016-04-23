using Dashboard.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace Dashboard.Helpers
{
    public sealed class StatsUtil
    {
        private static readonly object s_guard = new object();
        private static readonly Guid s_idBase = Guid.NewGuid();
        private static TestResultQueryCounterEntity s_entity;

        private readonly DashboardStorage _storage;

        public StatsUtil(DashboardStorage storage)
        {
            _storage = storage;
        }

        public void AddHit(bool isJenkins)
        {
            var entity = GetEntity();
            if (isJenkins)
            {
                entity.JenkinsHitCount++;
            }
            else
            {
                entity.NormalHitCount++;
            }

            Update(entity);
        }

        private void AddMiss(bool isJenkins)
        {
            var entity = GetEntity();
            if (isJenkins)
            {
                entity.JenkinsMissCount++;
            }
            else
            {
                entity.NormalMissCount++;
            }

            Update(entity);
        }

        private void Update(TestResultQueryCounterEntity entity)
        {
            var operation = TableOperation.InsertOrReplace(entity);
            _storage.TestResultQueryCounterTable.Execute(operation);
        }

        private TestResultQueryCounterEntity GetEntity()
        {
            var dateTime = DateTime.UtcNow;
            var entityWriterId = GetEntityWriterId();
            var ticks = TestResultQueryCounterEntity.GetTimeOfDayTicks(dateTime);
            var key = TestResultQueryCounterEntity.GetEntityKey(dateTime, entityWriterId);

            lock (s_guard)
            {
                if (s_entity == null ||
                    s_entity.PartitionKey != key.PartitionKey ||
                    s_entity.RowKey != key.RowKey ||
                    s_entity.TimeOfDayTicks != ticks)
                {
                    s_entity = TestResultQueryCounterEntity.Create(dateTime, entityWriterId);
                }

                return s_entity;
            }
        }

        private static string GetEntityWriterId() => $"{s_idBase}-{Thread.CurrentThread.ManagedThreadId}";
    }
}