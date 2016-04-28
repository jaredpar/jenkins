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
    internal sealed class BuildTablePopulator
    {
        private readonly CloudTable _buildResultDateTable;
        private readonly CloudTable _buildResultExactTable;
        private readonly CloudTable _buildFailureDateTable;
        private readonly CloudTable _buildFailureExactTable;
        private readonly JenkinsClient _client;
        private readonly TextWriter _textWriter;

        internal BuildTablePopulator(CloudTable buildResultDateTable, CloudTable buildResultExactTable, CloudTable buildFailureDateTable, CloudTable buildFailureExactTable, JenkinsClient client, TextWriter textWriter)
        {
            Debug.Assert(buildResultDateTable.Name == AzureConstants.TableNames.BuildResultDate);
            Debug.Assert(buildResultExactTable.Name == AzureConstants.TableNames.BuildResultExact);
            Debug.Assert(buildFailureDateTable.Name == AzureConstants.TableNames.BuildFailureDate);
            Debug.Assert(buildFailureExactTable.Name == AzureConstants.TableNames.BuildFailureExact);
            _buildResultDateTable = buildResultDateTable;
            _buildResultExactTable = buildResultExactTable;
            _buildFailureDateTable = buildFailureDateTable;
            _buildFailureExactTable = buildFailureExactTable;
            _client = client;
            _textWriter = textWriter;
        }

        /// <summary>
        /// Populate the <see cref="BuildResultEntity"/> structures for a build overwriting any data 
        /// that existed before.  Returns the entity if enough information was there to process the value.
        /// </summary>
        internal async Task<BuildResultEntity> PopulateBuild(BuildId buildId)
        {
            var entity = await PopulateBuildIdCore(buildId);
            if (entity == null)
            {
                return null;
            }

            entity.SetEntityKey(entity.GetExactEntityKey());
            await _buildResultExactTable.ExecuteAsync(TableOperation.InsertOrReplace(entity));
            entity.SetEntityKey(entity.GetDateEntityKey());
            await _buildResultDateTable.ExecuteAsync(TableOperation.InsertOrReplace(entity));
            return entity;
        }

        private async Task<BuildResultEntity> PopulateBuildIdCore(BuildId buildId)
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
        private async Task<BuildResultEntity> GetBuildFailureEntity(BuildId id)
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

            return new BuildResultEntity(buildInfo.Id, buildInfo.Date, buildInfo.MachineName, kind);
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

        private async Task PopulateUnitTestFailure(BuildId buildId, BuildInfo buildInfo)
        {
            var testCaseNames = _client.GetFailedTestCases(buildId);
            var entityList = testCaseNames
                .Select(x => BuildFailureEntity.CreateTestCaseFailure(buildInfo.Date, buildId, x, buildInfo.MachineName))
                .ToList();
            EnsureTestCaseNamesUnique(entityList);
            await AzureUtil.InsertBatchUnordered(_buildFailureExactTable, entityList);
            await AzureUtil.InsertBatchUnordered(_buildFailureDateTable, entityList.Select(x => new BuildFailureDateEntity(x)).ToList());
        }

        private void WriteLine(BuildId buildId, string message)
        {
            _textWriter.WriteLine($"{buildId.JobName} - {buildId.Id}: {message}");
        }

        /// <summary>
        /// It's possible, although technically invalid for a job to produce multiple test cases with 
        /// the same name.  Normalize those now because our table scheme requires them to be unique. 
        /// </summary>
        private static void EnsureTestCaseNamesUnique(List<BuildFailureExactEntity> list)
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
