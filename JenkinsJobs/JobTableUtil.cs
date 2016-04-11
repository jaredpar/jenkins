using Microsoft.WindowsAzure.Storage.Table;
using Roslyn.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JenkinsJobs
{
    internal sealed class JobTableUtil
    {
        // Current Azure Limit
        internal const int MaxBatchCount = 100;

        private readonly CloudTable _buildProcessedTable;
        private readonly CloudTable _buildFailureTable;
        private readonly RoslynClient _roslynClient;
        private readonly JenkinsClient _jenkinsClient;
        private readonly List<string> _errorList = new List<string>();

        internal List<string> ErrorList => _errorList;

        // TODO: consider the impact of parallel runs of this job run.  Perhaps just disallow for now.
        internal JobTableUtil(CloudTable buildProcessedTable, CloudTable buildFailureTable, RoslynClient roslynClient)
        {
            _buildProcessedTable = buildProcessedTable;
            _buildFailureTable = buildFailureTable;
            _roslynClient = roslynClient;
            _jenkinsClient = _roslynClient.Client;
        }

        internal async Task Populate()
        {
            _errorList.Clear();

            foreach (var jobName in _roslynClient.GetJobNames())
            {
                var buildIdList = _jenkinsClient.GetBuildIds(jobName);
                await PopulateBuildIds(jobName, buildIdList);
            }
        }

        private async Task PopulateBuildIds(string jobName, List<BuildId> buildIdList)
        {
            var oldProcessedList = GetBuildProcessedList(jobName);
            var newProcessedList = new List<BuildProcessedEntity>();

            foreach (var buildId in buildIdList)
            {
                if (oldProcessedList.Any(x => x.BuildId.Id == buildId.Id))
                {
                    continue;
                }

                try
                {
                    var succeeded = await PopulateBuildId(buildId);
                    var entity = new BuildProcessedEntity(buildId, succeeded);
                    newProcessedList.Add(entity);
                }
                catch (Exception ex)
                {
                    _errorList.Add($"Error processing {buildId.JobName} {buildId.Id}: {ex}");
                }
            }

            await InsertBatch(_buildProcessedTable, newProcessedList);
        }

        private List<BuildProcessedEntity> GetBuildProcessedList(string jobName)
        {
            var query = new TableQuery<BuildProcessedEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, jobName));
            var result = _buildProcessedTable.ExecuteQuery(query);
            return result.ToList();
        }

        private async Task<bool> PopulateBuildId(BuildId buildId)
        {
            var buildResult = _jenkinsClient.GetBuildResult(buildId);

            // Don't need to do any processing for successful build values.
            if (buildResult.Succeeded)
            {
                return true;
            }

            if (buildResult.FailureInfo?.Reason != BuildFailureReason.TestCase)
            {
                throw new Exception($"Unable to process {buildResult.FailureInfo?.Reason}");
            }

            await PopulateUnitTestFailure(buildId, buildResult.FailureInfo.Messages);
            return false;
        }

        private Task PopulateUnitTestFailure(BuildId buildId, List<string> testCaseNames)
        {
            var entityList = testCaseNames
                .Select(x => BuildFailureEntity.CreateUnitTestFailure(buildId, x))
                .ToList();
            return InsertBatch(_buildFailureTable, entityList);
        }

        private static async Task InsertBatch<T>(CloudTable table, List<T> entityList)
            where T : TableEntity
        {
            if (entityList.Count == 0)
            {
                return;
            }

            var operation = new TableBatchOperation();
            foreach (var entity in entityList)
            {
                operation.Insert(entity);

                if (operation.Count == MaxBatchCount)
                {
                    await table.ExecuteBatchAsync(operation);
                    operation.Clear();
                }
            }

            if (operation.Count > 0)
            {
                await table.ExecuteBatchAsync(operation);
            }
        }
    }
}
