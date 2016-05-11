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
using Microsoft.WindowsAzure.Storage;

namespace Dashboard.Azure
{
    public sealed class BuildTablePopulator
    {
        private readonly CloudTable _buildResultDateTable;
        private readonly CloudTable _buildResultExactTable;
        private readonly CloudTable _buildFailureDateTable;
        private readonly CloudTable _buildFailureExactTable;
        private readonly JenkinsClient _client;
        private readonly TextWriter _textWriter;

        public BuildTablePopulator(CloudTableClient tableClient, JenkinsClient client, TextWriter textWriter) :this(
            buildResultDateTable: tableClient.GetTableReference(AzureConstants.TableNames.BuildResultDate),
            buildResultExactTable: tableClient.GetTableReference(AzureConstants.TableNames.BuildResultExact),
            buildFailureDateTable: tableClient.GetTableReference(AzureConstants.TableNames.BuildFailureDate),
            buildFailureExactTable: tableClient.GetTableReference(AzureConstants.TableNames.BuildFailureExact),
            client: client,
            textWriter: textWriter)
        {

        }

        public BuildTablePopulator(CloudTable buildResultDateTable, CloudTable buildResultExactTable, CloudTable buildFailureDateTable, CloudTable buildFailureExactTable, JenkinsClient client, TextWriter textWriter)
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
        public async Task<BuildResultEntity> PopulateBuild(BuildId buildId)
        {
            var entity = await PopulateBuildIdCore(buildId);
            if (entity == null)
            {
                return null;
            }

            await _buildResultDateTable.ExecuteAsync(TableOperation.InsertOrReplace(entity.CopyDate()));
            await _buildResultExactTable.ExecuteAsync(TableOperation.InsertOrReplace(entity.CopyExact()));
            return entity;
        }

        private async Task<BuildResultEntity> PopulateBuildIdCore(BuildId buildId)
        {
            try
            {
                var entity = await GetBuildFailureEntity(buildId);
                WriteLine(buildId, $"adding reason {entity.ClassificationKind}");
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

            PullRequestInfo prInfo = null;
            if (JobUtil.IsPullRequestJobName(id.JobId.Name))
            {
                prInfo = await _client.GetPullRequestInfoAsync(id);
            }

            BuildResultClassification classification;
            switch (buildInfo.State)
            {
                case BuildState.Succeeded:
                    classification = BuildResultClassification.Succeeded;
                    break;
                case BuildState.Aborted:
                    classification = BuildResultClassification.Aborted;
                    break;
                case BuildState.Failed:
                    classification = await PopulateFailedBuildResult(buildInfo, prInfo);
                    break;
                case BuildState.Running:
                    classification = BuildResultClassification.Unknown;
                    break;
                default:
                    throw new Exception($"Invalid enum: {buildInfo.State} for {id.JobName} - {id.Number}");
            }

            return new BuildResultEntity(buildInfo.Id, buildInfo.Date, buildInfo.MachineName, classification, prInfo);
        }

        private async Task<BuildResultClassification> PopulateFailedBuildResult(BuildInfo buildInfo, PullRequestInfo prInfo)
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

            if (buildResult.FailureInfo == null)
            {
                return BuildResultClassification.Unknown;
            }

            var classification = BuildResultClassification.Unknown;
            foreach (var cause in buildResult.FailureInfo.CauseList)
            {
                var current = ConvertToClassification(cause);
                if (classification.Kind == ClassificationKind.Unknown || classification.Kind == ClassificationKind.MergeConflict)
                {
                    classification = current;
                }
            }

            if (classification.Kind == ClassificationKind.TestFailure)
            {
                await PopulateUnitTestFailure(buildInfo, prInfo);
            }

            return classification;
        }

        private BuildResultClassification ConvertToClassification(BuildFailureCause cause)
        {
            if (cause.Category == BuildFailureCause.CategoryMergeConflict)
            {
                return BuildResultClassification.MergeConflict;
            }

            if (cause.Category == BuildFailureCause.CategoryUnknown)
            {
                return BuildResultClassification.Unknown;
            }

            var category = cause.Category.ToLower();
            switch (category)
            {
                case "test":
                    return BuildResultClassification.TestFailure;
                case "build":
                    return BuildResultClassification.BuildFailure;
                case "infrastructure":
                    return BuildResultClassification.Infrastructure;
                default:
                    return new BuildResultClassification(ClassificationKind.Custom, cause.Category, cause.Name);
            }
        }

        private async Task PopulateUnitTestFailure(BuildInfo buildInfo, PullRequestInfo prInfo)
        {
            var buildId = buildInfo.Id;
            var testCaseNames = _client.GetFailedTestCases(buildId);
            var entityList = testCaseNames
                .Select(x => BuildFailureEntity.CreateTestCaseFailure(buildInfo.Date, buildId, x, buildInfo.MachineName, prInfo))
                .ToList();
            EnsureTestCaseNamesUnique(entityList);
            await AzureUtil.InsertBatchUnordered(_buildFailureExactTable, entityList.Select(x => x.CopyExact()));
            await AzureUtil.InsertBatchUnordered(_buildFailureDateTable, entityList.Select(x => x.CopyDate()));
        }

        private void WriteLine(BuildId buildId, string message)
        {
            _textWriter.WriteLine($"{buildId.JobName} - {buildId.Number}: {message}");
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
