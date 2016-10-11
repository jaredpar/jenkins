using Dashboard.Azure;
using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Xunit;

namespace Dashboard.Tests
{
    public class AzureUtilTests
    {
        public class ViewNameTests : AzureUtilTests
        {
            [Fact]
            public void Root()
            {
                Assert.Equal(AzureUtil.ViewNameRoot, AzureUtil.GetViewName(JobId.Root));
            }

            [Fact]
            public void Simple()
            {
                var jobId = JobId.ParseName("dog");
                Assert.Equal(AzureUtil.ViewNameRoot, AzureUtil.GetViewName(jobId));
            }

            [Fact]
            public void Nested()
            {
                var jobId = JobId.ParseName("house/dog");
                Assert.Equal("house", AzureUtil.GetViewName(jobId));
            }

            [Fact]
            public void VeryNested()
            {
                var jobId = JobId.ParseName("house/dog/lab");
                Assert.Equal("house", AzureUtil.GetViewName(jobId));
            }

            [Fact]
            public void Private()
            {
                var jobId = JenkinsUtil.ConvertPathToJobId("job/Private/job/dotnet_debuggertests/job/master/job/linux_dbg/");
                Assert.Equal("dotnet_debuggertests", AzureUtil.GetViewName(jobId));
            }

            /// <summary>
            /// Roslyn is a special case here.  We group the public and private jobs together.
            /// </summary>
            [Fact]
            public void PrivateRoslyn()
            {
                var jobId = JenkinsUtil.ConvertPathToJobId("job/Private/job/dotnet_roslyn-internal/job/master/job/windows_debug_eta/");
                Assert.Equal("dotnet_roslyn", AzureUtil.GetViewName(jobId));
            }
        }

        public sealed class BatchInsertTests : AzureUtilTests, IDisposable
        {
            private readonly CloudTable _table;

            public BatchInsertTests()
            {
                var account = Util.GetStorageAccount();
                var client = account.CreateCloudTableClient();
                _table = client.GetTableReference("BatchOperationTests");
                _table.CreateIfNotExists();
            }

            public void Dispose()
            {
                _table.Delete();
            }

            private static List<DynamicTableEntity> GetSamePartitionList(int count, string partitionKey)
            {
                var list = new List<DynamicTableEntity>();
                for (var i = 0; i < count; i++)
                {
                    var rowKey = i.ToString("0000");
                    var entity = new DynamicTableEntity()
                    {
                        PartitionKey = partitionKey,
                        RowKey = rowKey
                    };
                    list.Add(entity);
                }

                return list;
            }

            private static List<DynamicTableEntity> GetSameRowList(int count, string rowKey)
            {
                var list = new List<DynamicTableEntity>();
                for (var i = 0; i < count; i++)
                {
                    var partitionKey = i.ToString("0000");
                    var entity = new DynamicTableEntity()
                    {
                        PartitionKey = partitionKey,
                        RowKey = rowKey
                    };
                    list.Add(entity);
                }

                return list;
            }

            [Fact]
            public async Task InsertSamePartitionKey()
            {
                var key = "test";
                var count = AzureUtil.MaxBatchCount * 2;
                var list = GetSamePartitionList(count, key);
                await AzureUtil.InsertBatch(_table, list);

                var found = await AzureUtil.QueryAsync<DynamicTableEntity>(_table, TableQueryUtil.PartitionKey(key));
                Assert.Equal(count, found.Count);
            }

            [Fact]
            public async Task InsertSameRowKey()
            {
                var key = "test";
                var count = AzureUtil.MaxBatchCount * 2;
                var list = GetSameRowList(count, key);
                await AzureUtil.InsertBatchUnordered(_table, list);

                var found = await AzureUtil.QueryAsync<DynamicTableEntity>(_table, TableQueryUtil.RowKey(key));
                Assert.Equal(count, found.Count);
            }

            [Fact]
            public async Task DeleteBatchSamePartitionKey()
            {
                var key = "test";
                var count = AzureUtil.MaxBatchCount * 2;
                var list = GetSamePartitionList(count, key);
                await AzureUtil.InsertBatchUnordered(_table, list);
                await AzureUtil.DeleteBatch(_table, list.Select(x => x.GetEntityKey()));

                var found = await AzureUtil.QueryAsync<DynamicTableEntity>(_table, TableQueryUtil.RowKey(key));
                Assert.Equal(0, found.Count);
            }
        }

        public class MiscTests : AzureUtilTests
        {
            [Fact]
            public void NormalizeKey()
            {
                Assert.Equal("foo", AzureUtil.NormalizeKey("foo", '_'));
                Assert.Equal("foo_bar", AzureUtil.NormalizeKey("foo/bar", '_'));
            }
        }
    }
}
