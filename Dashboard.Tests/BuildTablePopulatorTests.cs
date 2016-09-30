using Dashboard.Azure;
using Dashboard.Azure.Builds;
using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dashboard.Tests
{
    public class BuildTablePopulatorTests
    {
        private readonly MockRestClient _restClient;
        private readonly BuildTablePopulator _populator;
        private readonly CloudTable _buildFailureExactTable;
        private readonly CloudTable _buildResultExactTable;

        public BuildTablePopulatorTests()
        {
            var account = Util.GetStorageAccount();
            var tableClient = account.CreateCloudTableClient();

            _restClient = new MockRestClient();
            var client = new JenkinsClient(SharedConstants.DotnetJenkinsUri, _restClient.Client);
            _buildFailureExactTable = tableClient.GetTableReference(AzureConstants.TableNames.BuildFailureExact);
            _buildResultExactTable = tableClient.GetTableReference(AzureConstants.TableNames.BuildResultExact);
            _populator = new BuildTablePopulator(
                tableClient,
                client: client,
                textWriter: new StringWriter());
        }

        [Fact]
        public async Task TaoFailure()
        {
            var buildId = new BuildId(4, JobId.ParseName("test"));
            _restClient.AddJson(
                buildId: buildId,
                buildResultJson: TestResources.Tao1BuildResult,
                buildInfoJson: TestResources.Tao1BuildInfo,
                failureInfoJson: TestResources.Tao1FailureInfo,
                testReportJson: TestResources.Tao1TestResult,
                jobXml: @"<freeStyleProject></freeStyleProject>");

            var entity = await _populator.PopulateBuild(buildId);

            var filter = TableQueryUtil.Column(nameof(BuildFailureEntity.JobName), buildId.JobName);
            var list = AzureUtil.Query<BuildFailureEntity>(_buildFailureExactTable, filter).ToList();
            Assert.Equal(2, list.Count);
            foreach (var item in list)
            {
                Assert.Equal(BuildFailureKind.TestCase, item.BuildFailureKind);
                Assert.Equal(buildId, item.BuildId);
            }
        }

        [Fact]
        public async Task IsPopulated()
        {
            var buildId = new BuildId(42, JobId.ParseName(Guid.NewGuid().ToString()));
            Assert.False(await _populator.IsPopulated(buildId));

            var key = BuildResultEntity.GetExactEntityKey(buildId);
            var entity = new DynamicTableEntity()
            {
                PartitionKey = key.PartitionKey,
                RowKey = key.RowKey
            };
            await _buildResultExactTable.ExecuteAsync(TableOperation.Insert(entity));
            Assert.True(await _populator.IsPopulated(buildId));
        }
    }
}
