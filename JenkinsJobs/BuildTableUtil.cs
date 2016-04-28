using Microsoft.WindowsAzure.Storage.Table;
using Dashboard.Azure;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Dashboard.StorageBuilder
{
    internal sealed class BuildTableUtil
    {
        private readonly CloudTable _buildResultDateTable;
        private readonly CloudTable _buildResultExactTable;
        private readonly CloudTable _buildFailureTable;
        private readonly JenkinsClient _client;
        private readonly TextWriter _textWriter;

        internal BuildTableUtil(CloudTable buildResultDateTable, CloudTable buildResultExactTable, CloudTable buildFailureTable, JenkinsClient client, TextWriter textWriter)
        {
            Debug.Assert(buildResultDateTable.Name == BuildResultDateEntity.TableName);
            Debug.Assert(buildResultExactTable.Name == BuildResultExactEntity.TableName);
            Debug.Assert(buildFailureTable.Name == BuildFailureEntity.TableName);
            _buildResultDateTable = buildResultDateTable;
            _buildResultExactTable = buildResultExactTable;
            _buildFailureTable = buildFailureTable;
            _client = client;
            _textWriter = textWriter;
        }

        /// <summary>
        /// Populate the <see cref="BuildResultEntityBase"/> structures for a build overwriting any data 
        /// that existed before.  Returns the entity if enough information was there to process the value.
        /// </summary>
        internal async Task<BuildResultExactEntity> PopulateBuild(BuildId buildId)
        {
            var entity = await PopulateBuildIdCore(buildId);
            if (entity == null)
            {
                return null;
            }

            await _buildResultExactTable.ExecuteAsync(TableOperation.InsertOrReplace(entity));
            await _buildResultDateTable.ExecuteAsync(TableOperation.InsertOrReplace(new BuildResultDateEntity(entity)));
            return entity;
        }

        private async Task<BuildResultExactEntity> PopulateBuildIdCore(BuildId buildId)
        {
            try
            {
                var entity = await GetBuildFailureEntity(buildId);
                WriteLine(buildId, $"adding reason {entity.BuildResultKind}");
                return entity;
            }
            catch (Exception ex)
            {
                WriteLine(buildId, $"error processing {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Update the table storage to contain the result of the specified build.
        /// </summary>
        private async Task<BuildResultExactEntity> GetBuildFailureEntity(BuildId id)
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

            return new BuildResultExactEntity(buildInfo.Date, buildInfo.Id, buildInfo.MachineName, kind);
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
                throw;
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
            EnsureTestCaseNamesUnique(entityList);
            return AzureUtil.InsertBatch(_buildFailureTable, entityList);
        }

        private void WriteLine(BuildId buildId, string message)
        {
            _textWriter.WriteLine($"{buildId.JobName} - {buildId.Id}: {message}");
        }

        /// <summary>
        /// It's possible, although technically invalid for a job to produce multiple test cases with 
        /// the same name.  Normalize those now because our table scheme requires them to be unique. 
        /// </summary>
        private static void EnsureTestCaseNamesUnique(List<BuildFailureEntity> list)
        {
            var set = new HashSet<string>();
            foreach (var entity in list)
            {
                var rowKey = entity.RowKey;
                var suffix = 0;
                while (!set.Add(entity.RowKey))
                {
                    entity.RowKey = $"{rowKey}_{suffix}";
                    suffix++;
                }
            }
        }
    }
}
