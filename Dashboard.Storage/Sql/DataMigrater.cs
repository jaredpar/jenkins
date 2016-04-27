using Dashboard;
using Dashboard.Azure;
using Dashboard.Sql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Sql
{
    // Migrate data from SQL to Table storage
    public sealed class DataMigrater
    {
        private readonly SqlUtil _sqlUtil;
        private readonly DashboardStorage _storage;

        public DataMigrater(string sqlConnectionString, string tableConnectionString)
        {
            _sqlUtil = new SqlUtil(sqlConnectionString);
            _storage = new DashboardStorage(tableConnectionString);
            AzureUtil.EnsureAzureResources(_storage.StorageAccount);
        }

        public async Task MigrateTestRun()
        {
            var testRunLegacyList = GetTestRuns();

            var entityList = new List<TestRunEntity>();
            foreach (var cur in testRunLegacyList)
            {
                var entity = new TestRunEntity(cur.RunDate.ToUniversalTime(), BuildSource.CreateAnonymous(Guid.NewGuid().ToString()));
                entity.CacheType = cur.Cache;
                entity.ElapsedSeconds = (long)cur.Elapsed.TotalSeconds;
                entity.Succeeded = cur.Succeeded;
                entity.IsJenkins = cur.IsJenkins;
                entity.Is32Bit = cur.Is32Bit;
                entity.AssemblyCount = cur.AssemblyCount;
                entity.ChunkCount = cur.ChunkCount;
                entity.CacheCount = cur.CacheCount;
                entityList.Add(entity);
            }

            var table = _storage.StorageAccount.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.TestRunData);
            foreach (var group in entityList.GroupBy(x => x.PartitionKey))
            {
                Console.WriteLine($"Group {group.Key}");
                await AzureUtil.InsertBatch(table, group.ToList());
            }
        }

        private List<TestRunLegacy> GetTestRuns()
        {
            return _sqlUtil.GetTestRuns(startDateTime: new DateTime(year: 2016, month: 1, day: 1), endDateTime: DateTime.UtcNow);
        }

        /// <summary>
        /// Migrates hit and miss but not store count.
        /// </summary>
        /// <returns></returns>
        public async Task MigrateTestCacheCounter1()
        {
            var entityWriterId = Guid.NewGuid().ToString();
            var map = new Dictionary<EntityKey, TestCacheCounterEntity>();
            var commandText = @"
                SELECT QueryDate, IsHit, IsJenkins
                FROM dbo.TestResultQueries";
            using (var command = new SqlCommand(commandText, _sqlUtil.Connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var queryDate = reader.GetDateTime(0);
                        var isHit = reader.GetBoolean(1);
                        var isJenkins = reader.GetBoolean(1);

                        var counterData = new CounterData(queryDate.ToUniversalTime(), entityWriterId, isJenkins);
                        TestCacheCounterEntity entity;
                        if (!map.TryGetValue(counterData.EntityKey, out entity))
                        {
                            entity = new TestCacheCounterEntity(counterData);
                            map[counterData.EntityKey] = entity;
                        }

                        if (isHit)
                        {
                            entity.HitCount++;
                        }
                        else
                        {
                            entity.MissCount++;
                        }
                    }
                }
            }

            var table = _storage.StorageAccount.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.TestCacheCounter);
            foreach (var group in map.Values.GroupBy(x => x.PartitionKey))
            {
                await AzureUtil.InsertBatch(table, group.ToList());
            }
        }

        // Migrate the StoreCount
        public async Task MigrateTestCacheCounter2()
        {
            var entityWriterId = Guid.NewGuid().ToString();
            var map = new Dictionary<EntityKey, TestCacheCounterEntity>();
            var commandText = @"
                SELECT StoreDate
                FROM dbo.TestResultStore";
            using (var command = new SqlCommand(commandText, _sqlUtil.Connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0))
                        {
                            continue;
                        }

                        var storeDate = reader.GetDateTime(0);

                        var counterData = new CounterData(storeDate.ToUniversalTime(), entityWriterId, isJenkins: false);
                        TestCacheCounterEntity entity;
                        if (!map.TryGetValue(counterData.EntityKey, out entity))
                        {
                            entity = new TestCacheCounterEntity(counterData);
                            map[counterData.EntityKey] = entity;
                        }

                        entity.StoreCount++;
                    }
                }
            }

            var table = _storage.StorageAccount.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.TestCacheCounter);
            foreach (var group in map.Values.GroupBy(x => x.PartitionKey))
            {
                await AzureUtil.InsertBatch(table, group.ToList());
            }

        }

        public async Task MigrateTestRunCounter()
        {
            var entityWriterId = Guid.NewGuid().ToString();
            var map = new Dictionary<EntityKey, TestRunCounterEntity>();
            foreach (var testRun in GetTestRuns())
            {
                var counterData = new CounterData(testRun.RunDate.ToUniversalTime(), entityWriterId, testRun.IsJenkins);
                TestRunCounterEntity entity;
                if (!map.TryGetValue(counterData.EntityKey, out entity))
                {
                    entity = new TestRunCounterEntity(counterData);
                    map[entity.GetEntityKey()] = entity;
                }

                entity.RunCount++;
                
                if (testRun.Succeeded)
                {
                    entity.SucceededCount++;
                }
            }

            var table = _storage.StorageAccount.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.TestRunCounter);
            foreach (var group in map.Values.GroupBy(x => x.PartitionKey))
            {
                await AzureUtil.InsertBatch(table, group.ToList());
            }
        }

        public async Task MigrateUnitTestData()
        {
            var entityWriterId = Guid.NewGuid().ToString();
            var map = new Dictionary<EntityKey, UnitTestCounterEntity>();
            var commandText = @"
                SELECT QueryDate, Passed, Failed, Skipped, ElapsedSeconds
                FROM dbo.TestResultQueries
                INNER JOIN dbo.TestResultStore
                ON dbo.TestResultQueries.Checksum = dbo.TestResultStore.Checksum
                WHERE IsHit = 1";
            using (var command = new SqlCommand(commandText, _sqlUtil.Connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(1))
                        {
                            continue;
                        }

                        var queryDate = reader.GetDateTime(0);
                        var passed = reader.GetInt32(1);
                        var failed = reader.GetInt32(2);
                        var skipped = reader.GetInt32(3);
                        var elapsed = reader.GetInt32(4);
                        var counterData = new CounterData(queryDate.ToUniversalTime(), entityWriterId, isJenkins: false);
                        UnitTestCounterEntity entity;
                        if (!map.TryGetValue(counterData.EntityKey, out entity))
                        {
                            entity = new UnitTestCounterEntity(counterData);
                            map[counterData.EntityKey] = entity;
                        }

                        entity.AssemblyCount++;
                        entity.TestsPassed += passed;
                        entity.TestsFailed += failed;
                        entity.TestsSkipped += skipped;
                        entity.ElapsedSeconds += elapsed;
                    }
                }
            }

            var table = _storage.StorageAccount.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.UnitTestQueryCounter);
            foreach (var group in map.Values.GroupBy(x => x.PartitionKey))
            {
                await AzureUtil.InsertBatch(table, group.ToList());
            }
        }
    }
}
