using Dashboard.Azure;
using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dashboard.Tests
{
    public class BuildTablePopulatorTests
    {
        private readonly MockRestClient _restClient;
        private readonly BuildTablePopulator _populator;
        private readonly CloudTable _buildFailureExactTable;

        public BuildTablePopulatorTests()
        {
            var account = Util.GetStorageAccount();
            var tableClient = account.CreateCloudTableClient();

            _restClient = new MockRestClient();
            var client = new JenkinsClient(SharedConstants.DotnetJenkinsUri, _restClient.Client);
            _buildFailureExactTable = tableClient.GetTableReference(AzureConstants.TableNames.BuildFailureExact);
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
                testReportJson: TestResources.Tao1TestResult);

            var entity = await _populator.PopulateBuild(buildId);

            var filter = FilterUtil
                .Column(nameof(BuildFailureEntity.JobName), buildId.JobName)
                .Filter;
            var list = AzureUtil.Query<BuildFailureEntity>(_buildFailureExactTable, filter).ToList();
            Assert.Equal(2, list.Count);
            foreach (var item in list)
            {
                Assert.Equal(BuildFailureKind.TestCase, item.BuildFailureKind);
                Assert.Equal(buildId, item.BuildId);
            }
        }
    }
}
