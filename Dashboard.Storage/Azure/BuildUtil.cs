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

        public List<BuildResultEntity> GetBuildResults(DateTimeOffset startDate, string viewName)
        {
            var filter = FilterUtil.SinceDate(ColumnNames.PartitionKey, startDate);
            filter = FilterView(filter, viewName);
            var query = new TableQuery<BuildResultEntity>().Where(filter);
            return _buildResultDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildResultEntity> GetBuildResults(DateTimeOffset startDate, string jobName, string viewName)
        {
            var filter = FilterUtil
                .SinceDate(ColumnNames.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildResultEntity.JobName), jobName));
            filter = FilterView(filter, viewName);
            var query = new TableQuery<BuildResultEntity>().Where(filter);
            return _buildResultDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildResultEntity> GetBuildResults(DateTimeOffset startDate, ClassificationKind kind, string viewName)
        {
            var filter = FilterUtil
                .SinceDate(ColumnNames.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildResultEntity.ClassificationKindRaw), kind.ToString()));
            filter = FilterView(filter, viewName);
            var query = new TableQuery<BuildResultEntity>().Where(filter);
            return _buildResultDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildFailureEntity> GetTestCaseFailures(DateTimeOffset startDate, string viewName)
        {
            var filter = FilterUtil
                .SinceDate(ColumnNames.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildFailureEntity.BuildFailureKindRaw), BuildFailureKind.TestCase.ToString()));
            filter = FilterView(filter, viewName);
            var query = new TableQuery<BuildFailureEntity>().Where(filter);
            return _buildFailureDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildFailureEntity> GetTestCaseFailures(DateTimeOffset startDate, string name, string viewName)
        {
            var filter = FilterUtil
                .SinceDate(ColumnNames.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildFailureEntity.Identifier), name));
            filter = FilterView(filter, viewName);
            var query = new TableQuery<BuildFailureEntity>().Where(filter);
            return _buildFailureDateTable.ExecuteQuery(query).ToList();
        }

        private static FilterUtil FilterView(FilterUtil util, string viewName)
        {
            Debug.Assert(nameof(BuildResultEntity.ViewName) == nameof(BuildFailureEntity.ViewName));

            if (viewName == AzureUtil.ViewNameAll)
            {
                return util;
            }

            var other = FilterUtil.Column(nameof(BuildResultEntity.ViewName), viewName, ColumnOperator.Equal);
            return util.And(other);
        }
    }
}
