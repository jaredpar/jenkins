﻿using Dashboard.Helpers;
using Dashboard.Models;
using Dashboard;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.WindowsAzure;
using Dashboard.Azure;
using Microsoft.WindowsAzure.Storage.Table;

namespace Dashboard.Controllers
{
    public sealed class TestRunData
    {
        public string Cache { get; set; }
        public int ElapsedSeconds { get; set; }
        public bool Succeeded { get; set; }
        public bool IsJenkins { get; set; }
        public bool Is32Bit { get; set; }
        public int AssemblyCount { get; set; }
        public int CacheCount { get; set; }
        public int ChunkCount { get; set; }

        // Misspelled version to keep until we can flow throw all of the spelling updates.
        public int EllapsedSeconds { get; set; }
    }

    public class TestRunController : DashboardApiController
    {
        private readonly TestCacheStats _stats;
        private readonly CounterStatsUtil _statsUtil;
        private readonly TestResultStorage _storage;
        private readonly CloudTable _testRunTable;

        public TestRunController()
        {
            var storage = new TestResultStorage(Storage);
            _stats = new TestCacheStats(storage);
            _statsUtil = new CounterStatsUtil(Storage);
            _storage = new TestResultStorage(Storage);
            _testRunTable = StorageAccount.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.TestRunData);
        }

        public void Post([FromBody] TestRunData testRunData)
        {
            var elapsed = testRunData.ElapsedSeconds > 0
                ? testRunData.ElapsedSeconds
                : testRunData.EllapsedSeconds;

            // TODO: Need to send along build source from the test runner.
            var buildSource = BuildSource.CreateAnonymous();
            var runDate = DateTime.UtcNow;
            var entity = new TestRunEntity(runDate, buildSource)
            {
                CacheType = testRunData.Cache,
                ElapsedSeconds = elapsed,
                Succeeded = testRunData.Succeeded,
                IsJenkins = testRunData.IsJenkins,
                Is32Bit = testRunData.Is32Bit,
                CacheCount = testRunData.CacheCount,
                ChunkCount = testRunData.ChunkCount,
                AssemblyCount = testRunData.AssemblyCount
            };

            _statsUtil.AddTestRun(entity.Succeeded, entity.IsJenkins);
            var operation = TableOperation.Insert(entity);
            _testRunTable.Execute(operation);
        }
    }
}