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
        private readonly CloudTable _viewNameDateTable;

        public BuildUtil(CloudStorageAccount account)
        {
            var client = account.CreateCloudTableClient();
            _buildResultDateTable = client.GetTableReference(AzureConstants.TableNames.BuildResultDate);
            _buildFailureDateTable = client.GetTableReference(AzureConstants.TableNames.BuildFailureDate);
            _viewNameDateTable = client.GetTableReference(AzureConstants.TableNames.ViewNameDate);
        }

        public List<BuildResultEntity> GetBuildResults(DateTimeOffset startDate, string viewName)
        {
            var filter = FilterUtil.SinceDate(ColumnName.PartitionKey, startDate);
            filter = FilterView(filter, viewName);
            var query = new TableQuery<BuildResultEntity>().Where(filter);
            return _buildResultDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildResultEntity> GetBuildResults(DateTimeOffset startDate, string jobName, string viewName)
        {
            var filter = FilterUtil
                .SinceDate(ColumnName.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildResultEntity.JobName), jobName));
            filter = FilterView(filter, viewName);
            var query = new TableQuery<BuildResultEntity>().Where(filter);
            return _buildResultDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildResultEntity> GetBuildResultsByKindName(DateTimeOffset startDate, string kindName, string viewName)
        {
            var filter = FilterUtil
                .SinceDate(ColumnName.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildResultEntity.ClassificationName), kindName));
            filter = FilterView(filter, viewName);
            var query = new TableQuery<BuildResultEntity>().Where(filter);
            return _buildResultDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildFailureEntity> GetTestCaseFailures(DateTimeOffset startDate, string viewName)
        {
            var filter = FilterUtil
                .SinceDate(ColumnName.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildFailureEntity.BuildFailureKindRaw), BuildFailureKind.TestCase.ToString()));
            filter = FilterView(filter, viewName);
            var query = new TableQuery<BuildFailureEntity>().Where(filter);
            return _buildFailureDateTable.ExecuteQuery(query).ToList();
        }

        public List<BuildFailureEntity> GetTestCaseFailures(DateTimeOffset startDate, string name, string viewName)
        {
            var filter = FilterUtil
                .SinceDate(ColumnName.PartitionKey, startDate)
                .And(FilterUtil.Column(nameof(BuildFailureEntity.Identifier), name));
            filter = FilterView(filter, viewName);
            var query = new TableQuery<BuildFailureEntity>().Where(filter);
            return _buildFailureDateTable.ExecuteQuery(query).ToList();
        }

        public List<string> GetViewNames(DateTimeOffset startDate)
        {
            var query = new TableQuery<ViewNameEntity>().Where(FilterUtil.SinceDate(ColumnName.PartitionKey, startDate));
            var viewNameList = _viewNameDateTable.ExecuteQuery(query);

            var list = new List<string>();
            list.Add(AzureUtil.ViewNameAll);
            list.AddRange(viewNameList.Select(x => x.ViewName).Distinct());
            return list;
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
