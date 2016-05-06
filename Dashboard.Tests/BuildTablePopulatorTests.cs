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
        private readonly CloudTable _buildResultDateTable;
        private readonly CloudTable _buildResultExactTable;
        private readonly CloudTable _buildFailureDateTable;
        private readonly CloudTable _buildFailureExactTable;

        public BuildTablePopulatorTests()
        {
            // This is using the storage emulator account.  Make sure to run the following before starting
            // "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" start
            var account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            AzureUtil.EnsureAzureResources(account);

            var tableClient = account.CreateCloudTableClient();
            _buildResultDateTable = tableClient.GetTableReference(AzureConstants.TableNames.BuildResultDate);
            _buildResultExactTable = tableClient.GetTableReference(AzureConstants.TableNames.BuildResultExact);
            _buildFailureDateTable = tableClient.GetTableReference(AzureConstants.TableNames.BuildFailureDate);
            _buildFailureExactTable = tableClient.GetTableReference(AzureConstants.TableNames.BuildFailureExact);

            _restClient = new MockRestClient();
            var client = new JenkinsClient(SharedConstants.DotnetJenkinsUri, _restClient.Client);

            _populator = new BuildTablePopulator(
                buildResultDateTable: _buildResultDateTable,
                buildResultExactTable: _buildResultExactTable,
                buildFailureDateTable: _buildFailureDateTable,
                buildFailureExactTable: _buildFailureExactTable,
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
