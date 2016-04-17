using Microsoft.WindowsAzure.Storage.Table;
using Dashboard.Azure;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly JenkinsClient _client;
        private readonly TextWriter _textWriter;
        private readonly List<BuildAnalyzeError> _buildAnalyzeErrors = new List<BuildAnalyzeError>();

        internal List<BuildAnalyzeError> BuildAnalyzeErrors => _buildAnalyzeErrors;

        // TODO: consider the impact of parallel runs of this job run.  Perhaps just disallow for now.
        internal JobTableUtil(CloudTable buildProcessedTable, CloudTable buildFailureTable, JenkinsClient client, TextWriter textWriter)
        {
            _buildProcessedTable = buildProcessedTable;
            _buildFailureTable = buildFailureTable;
            _client = client;
            _textWriter = textWriter;
        }

        internal async Task MoveUnknownToIgnored()
        {
            var kindFilter = TableQuery.GenerateFilterCondition(
                nameof(BuildProcessedEntity.KindRaw),
                QueryComparisons.Equal,
                BuildResultKind.UnknownFailure.ToString());
            var dateFilter = TableQuery.GenerateFilterConditionForDate(
                nameof(BuildProcessedEntity.BuildDate),
                QueryComparisons.LessThanOrEqual,
                DateTimeOffset.UtcNow - TimeSpan.FromDays(1));
            var query = new TableQuery<BuildProcessedEntity>()
                .Where(TableQuery.CombineFilters(kindFilter, TableOperators.And, dateFilter));

            foreach (var entity in _buildProcessedTable.ExecuteQuery(query))
            {
                entity.KindRaw = BuildResultKind.IgnoredFailure.ToString();
                var operation = TableOperation.Replace(entity);
                await _buildProcessedTable.ExecuteAsync(operation);
                WriteLine(entity.BuildId, "moved to ignored");
            }
        }

        internal async Task Populate()
        {
            _buildAnalyzeErrors.Clear();

            foreach (var jobId in _client.GetJobIdsInView("Roslyn"))
            {
                _textWriter.WriteLine($"Processing {jobId.Name}");
                var buildIdList = _client.GetBuildIds(jobId);
                await PopulateBuildIds(jobId, buildIdList);
            }
        }

        private async Task PopulateBuildIds(JobId jobId, List<BuildId> buildIdList)
        {
            var oldProcessedList = GetBuildProcessedList(jobId);
            var newProcessedList = new List<BuildProcessedEntity>();

            foreach (var buildId in buildIdList)
            {
                // Don't want to reprocess build failures that we've already seen.  Must continue though if 
                // the job previously had an unknown failure or was listed as running.  In either case it needs
                // to be reprocessed to see if we can identify the failure.
                var oldEntity = oldProcessedList.FirstOrDefault(x => x.BuildId.Id == buildId.Id);
                if (oldEntity != null && !ShouldProcessExisting(oldEntity.Kind))
                {
                    continue;
                }

                try
                {
                    var entity = await GetBuildFailureEntity(buildId);

                    if (oldEntity != null && entity.Kind == oldEntity.Kind)
                    {
                        WriteLine(buildId, $"still in state {entity.Kind}");
                        continue;
                    }

                    WriteLine(buildId, $"adding reason {entity.Kind}");
                    newProcessedList.Add(entity);
                }
                catch (Exception ex)
                {
                    WriteLine(buildId, $"error processing {ex.Message}");
                    _buildAnalyzeErrors.Add(new BuildAnalyzeError(buildId, ex));
                }
            }

            await InsertBatch(_buildProcessedTable, newProcessedList);
        }

        /// <summary>
        /// Should a build which was previously processed with the specified result be processed 
        /// again? 
        /// </summary>
        private static bool ShouldProcessExisting(BuildResultKind kind)
        {
            return
                kind == BuildResultKind.Running ||
                kind == BuildResultKind.UnknownFailure ||
                kind == BuildResultKind.AnalyzeError;
        }

        private List<BuildProcessedEntity> GetBuildProcessedList(JobId id)
        {
            // TODO: should optimize this so we don't bring down so many rows and columns.
            var query = new TableQuery<BuildProcessedEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, id.Name));
            var result = _buildProcessedTable.ExecuteQuery(query);
            return result.ToList();
        }

        /// <summary>
        /// Update the table storage to contain the result of the specified build.
        /// </summary>
        private async Task<BuildProcessedEntity> GetBuildFailureEntity(BuildId id)
        {
            var buildInfo = _client.GetBuildInfo(id); 
            BuildResultKind kind;
            switch (buildInfo.State)
            {
                case BuildState.Succeeded:
                    kind = BuildResultKind.Succeeded;
                    break;
                case BuildState.Aborted:
                    kind = BuildResultKind.Aborted;
                    break;
                case BuildState.Failed:
                    kind = await PopulateFailedBuildResult(buildInfo);
                    break;
                case BuildState.Running:
                    kind = BuildResultKind.Running;
                    break;
                default:
                    throw new Exception($"Invalid enum: {buildInfo.State} for {id.JobName} - {id.Id}");
            }

            return new BuildProcessedEntity(id, buildInfo.Date, kind);
        }

        private async Task<BuildResultKind> PopulateFailedBuildResult(BuildInfo buildInfo)
        {
            var buildId = buildInfo.Id;
            BuildResult buildResult;
            try
            {
                buildResult = await _client.GetBuildResultAsync(buildInfo);
            }
            catch (Exception ex)
            {
                WriteLine(buildId, $"error getting build result {ex.Message}");
                _buildAnalyzeErrors.Add(new BuildAnalyzeError(buildId, ex));
                return BuildResultKind.AnalyzeError;
            }

            var category = buildResult.FailureInfo?.Category ?? BuildFailureCategory.Unknown;
            switch (category)
            {
                case BuildFailureCategory.Unknown:
                    return BuildResultKind.UnknownFailure;
                case BuildFailureCategory.NuGet:
                    return BuildResultKind.NuGetFailure;
                case BuildFailureCategory.Build:
                    return BuildResultKind.BuildFailure;
                case BuildFailureCategory.Infrastructure:
                    return BuildResultKind.InfrastructureFailure;
                case BuildFailureCategory.MergeConflict:
                    return BuildResultKind.MergeConflict;
                case BuildFailureCategory.TestCase:
                    await PopulateUnitTestFailure(buildId, buildResult.BuildInfo);
                    return BuildResultKind.UnitTestFailure;
                default:
                    throw new Exception($"Invalid enum value: {category}");
            }
        }

        private Task PopulateUnitTestFailure(BuildId buildId, BuildInfo buildInfo)
        {
            var testCaseNames = _client.GetFailedTestCases(buildId);
            var entityList = testCaseNames
                .Select(x => BuildFailureEntity.CreateTestCaseFailure(buildId, x, buildInfo.Date, buildInfo.MachineName))
                .ToList();
            return InsertBatch(_buildFailureTable, entityList);
        }

        private void WriteLine(BuildId buildId, string message)
        {
            _textWriter.WriteLine($"{buildId.JobName} - {buildId.Id}: {message}");
        }

        /// <summary>
        /// Inserts a collection of <see cref="TableEntity"/> values into a table using batch style 
        /// operations.  All entities must be insertable via batch operations.
        /// </summary>
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
                // Important to use InsertOrReplace here.  It's possible for a populate job to be cut off in the 
                // middle when the BuildFailure table is updated but not yet the BuildProcessed table.  Hence 
                // we'll up here again doing a batch insert.
                operation.InsertOrReplace(entity);

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
