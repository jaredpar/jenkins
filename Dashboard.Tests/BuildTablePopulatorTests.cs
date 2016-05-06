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

            // TODO: Using a live connection to Jenkins is terrible.  But it gets me coverage in the short term.  Long
            // term need to mock the requests.
            var githubConnectionString = ConfigurationManager.AppSettings[SharedConstants.GithubConnectionStringName];
            var client = new JenkinsClient(SharedConstants.DotnetJenkinsUri, githubConnectionString);

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
            var buildId = new BuildId(4, JobId.ParseName("roslyn_prtest_win_vsi0"));
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
