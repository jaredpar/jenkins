using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var filter = FilterRoslyn(FilterUtil.SinceDate(ColumnNames.PartitionKey, startDate)).Filter;
            var query = new TableQuery<BuildResultEntity>().Where(filter);
            return _buildResultDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildResultEntity> GetBuildResults(DateTimeOffset startDate, string jobName)
        {
            var filter = FilterRoslyn(FilterUtil
                .SinceDate(ColumnNames.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildResultEntity.JobName), jobName)))
                .Filter;
            var query = new TableQuery<BuildResultEntity>().Where(filter);
            return _buildResultDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildResultEntity> GetBuildResults(DateTimeOffset startDate, ClassificationKind kind)
        {
            var filter = FilterRoslyn(FilterUtil
                .SinceDate(ColumnNames.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildResultEntity.ClassificationKindRaw), kind.ToString())))
                .Filter;
            var query = new TableQuery<BuildResultEntity>().Where(filter);
            return _buildResultDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildFailureEntity> GetTestCaseFailures(DateTimeOffset startDate)
        {
            var filter = FilterRoslyn(FilterUtil
                .SinceDate(ColumnNames.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildFailureEntity.BuildFailureKindRaw), BuildFailureKind.TestCase.ToString())))
                .Filter;
            var query = new TableQuery<BuildFailureEntity>().Where(filter);
            return _buildFailureDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildFailureEntity> GetTestCaseFailures(DateTimeOffset startDate, string name)
        {
            var filter = FilterRoslyn(FilterUtil
                .SinceDate(ColumnNames.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildFailureEntity.Identifier), name)))
                .Filter;
            var query = new TableQuery<BuildFailureEntity>().Where(filter);
            return _buildFailureDateTable.ExecuteQuery(query).ToList();
        }

        private static FilterUtil FilterRoslyn(FilterUtil util)
        {
            Debug.Assert(nameof(BuildResultEntity.ViewName) == nameof(BuildFailureEntity.ViewName));
            var other = FilterUtil.Column(nameof(BuildResultEntity.ViewName), "dotnet_roslyn", ColumnOperator.Equal);
            return util.And(other);
        }
    }
}
