using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    // TODO: make methods async?
    public sealed class BuildUtil
    {
        private readonly CloudTable _buildResultDateTable;
        private readonly CloudTable _buildFailureDateTable;

        public BuildUtil(CloudStorageAccount account)
        {
            var client = account.CreateCloudTableClient();
            _buildResultDateTable = client.GetTableReference(AzureConstants.TableNames.BuildResultDate);
            _buildFailureDateTable = client.GetTableReference(AzureConstants.TableNames.BuildFailureDate);
        }

        public List<BuildResultEntity> GetBuildResults(DateTimeOffset startDate)
        {
            var filter = FilterUtil.SinceDate(ColumnNames.PartitionKey, startDate).Filter;
            var query = new TableQuery<BuildResultEntity>().Where(filter);
            return _buildResultDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildResultEntity> GetBuildResults(DateTimeOffset startDate, string jobName)
        {
            var filter = FilterUtil
                .SinceDate(ColumnNames.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildResultEntity.JobName), jobName))
                .Filter;
            var query = new TableQuery<BuildResultEntity>().Where(filter);
            return _buildResultDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildResultEntity> GetBuildResults(DateTimeOffset startDate, ClassificationKind kind)
        {
            var filter = FilterUtil
                .SinceDate(ColumnNames.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildResultEntity.ClassificationKindRaw), kind.ToString()))
                .Filter;
            var query = new TableQuery<BuildResultEntity>().Where(filter);
            return _buildResultDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildFailureEntity> GetTestCaseFailures(DateTimeOffset startDate)
        {
            var filter = FilterUtil
                .SinceDate(ColumnNames.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildFailureEntity.BuildFailureKindRaw), BuildFailureKind.TestCase.ToString()))
                .Filter;
            var query = new TableQuery<BuildFailureEntity>().Where(filter);
            return _buildFailureDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildFailureEntity> GetTestCaseFailures(DateTimeOffset startDate, string name)
        {
            var filter = FilterUtil
                .SinceDate(ColumnNames.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildFailureEntity.Identifier), name))
                .Filter;
            var query = new TableQuery<BuildFailureEntity>().Where(filter);
            return _buildFailureDateTable.ExecuteQuery(query).ToList();
        }
    }
}
