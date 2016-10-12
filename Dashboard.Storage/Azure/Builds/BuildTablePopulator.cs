using Microsoft.WindowsAzure.Storage.Table;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;
using System.Threading;

namespace Dashboard.Azure.Builds
{
    public sealed class BuildTablePopulator
    {
        private struct PopulateData
        {
            internal BuildResultEntity Result { get; }
            internal List<BuildFailureEntity> Failures { get; }

            internal PopulateData(BuildResultEntity result, List<BuildFailureEntity> failures)
            {
                Result = result;
                Failures = failures;
            }
        }

        private readonly CloudTable _buildResultDateTable;
        private readonly CloudTable _buildResultExactTable;
        private readonly CloudTable _buildFailureDateTable;
        private readonly CloudTable _buildFailureExactTable;
        private readonly CloudTable _viewNameDateTable;

        // TODO: This should not be a field.  A single populator can server several BoundBuildId that have different
        // hostname values.
        private readonly JenkinsClient _client;
        private readonly TextWriter _textWriter;

        public BuildTablePopulator(CloudTableClient tableClient, JenkinsClient client, TextWriter textWriter) : this(
            buildResultDateTable: tableClient.GetTableReference(AzureConstants.TableNames.BuildResultDate),
            buildResultExactTable: tableClient.GetTableReference(AzureConstants.TableNames.BuildResultExact),
            buildFailureDateTable: tableClient.GetTableReference(AzureConstants.TableNames.BuildFailureDate),
            buildFailureExactTable: tableClient.GetTableReference(AzureConstants.TableNames.BuildFailureExact),
            viewNameDateTable: tableClient.GetTableReference(AzureConstants.TableNames.ViewNameDate),
            client: client,
            textWriter: textWriter)
        {

        }

        public BuildTablePopulator(CloudTable buildResultDateTable, CloudTable buildResultExactTable, CloudTable buildFailureDateTable, CloudTable buildFailureExactTable, CloudTable viewNameDateTable, JenkinsClient client, TextWriter textWriter)
        {
            Debug.Assert(buildResultDateTable.Name == AzureConstants.TableNames.BuildResultDate);
            Debug.Assert(buildResultExactTable.Name == AzureConstants.TableNames.BuildResultExact);
            Debug.Assert(buildFailureDateTable.Name == AzureConstants.TableNames.BuildFailureDate);
            Debug.Assert(buildFailureExactTable.Name == AzureConstants.TableNames.BuildFailureExact);
            Debug.Assert(viewNameDateTable.Name == AzureConstants.TableNames.ViewNameDate);
            _buildResultDateTable = buildResultDateTable;
            _buildResultExactTable = buildResultExactTable;
            _buildFailureDateTable = buildFailureDateTable;
            _buildFailureExactTable = buildFailureExactTable;
            _viewNameDateTable = viewNameDateTable;
            _client = client;
            _textWriter = textWriter;
        }

        /// <summary>
        /// Is this build alreadiy fully populated.
        /// </summary>
        public async Task<bool> IsPopulated(BuildId buildId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var key = BuildResultEntity.GetExactEntityKey(buildId);
            var entity = await AzureUtil.QueryAsync<DynamicTableEntity>(_buildResultExactTable, key, cancellationToken);
            return entity != null;
        }

        /// <summary>
        /// Populate the <see cref="BuildResultEntity"/> structures for a build overwriting any data 
        /// that existed before.  Returns the entity if enough information was there to process the value.
        /// </summary>
        public async Task PopulateBuild(BoundBuildId buildId)
        {
            var data = await GetPopulateData(buildId);

            var result = data.Result;
            await PopulateViewName(buildId.JobId, result.BuildDateTimeOffset);
            await _buildResultDateTable.ExecuteAsync(TableOperation.InsertOrReplace(result.CopyDate()));
            await _buildResultExactTable.ExecuteAsync(TableOperation.InsertOrReplace(result.CopyExact()));

            var failures = data.Failures;
            if (failures.Count > 0)
            {
                // Important to use InsertOrReplace here.  It's possible for a populate job to be cut off in the 
                // middle when the BuildFailure table is updated but not yet the BuildProcessed table.  Hence 
                // we'll up here again doing a batch insert.
                await AzureUtil.ExecuteBatchUnordered(_buildFailureExactTable, TableOperationType.InsertOrReplace, failures.Select(x => x.CopyExact()));
                await AzureUtil.ExecuteBatchUnordered(_buildFailureDateTable, TableOperationType.InsertOrReplace, failures.Select(x => x.CopyDate()));
            }
        }

        public async Task PopulateBuildMissing(BoundBuildId buildId)
        {
            // The bulid is now deemed to be missing.  Finish it off.
            var result = new BuildResultEntity(
                buildId,
                DateTimeOffset.UtcNow,
                TimeSpan.MinValue,
                JobKind.Normal,
                "",
                BuildResultClassification.Infrastructure,
                prInfo: null);

            // Deliberately using Insert here vs. InsertOrReplace.  This is the worst possible outcome for this
            // type so let anyone else win
            await _buildResultDateTable.ExecuteAsync(TableOperation.Insert(result.CopyDate()));
            await _buildResultDateTable.ExecuteAsync(TableOperation.Insert(result.CopyExact()));
        }

        private async Task<PopulateData> GetPopulateData(BoundBuildId buildId)
        {
            try
            {
                var data = await GetPopulateDataCore(buildId);
                WriteLine(buildId, $"adding reason {data.Result.ClassificationKind}");
                return data;
            }
            catch (Exception ex)
            {
                WriteLine(buildId, $"error processing {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Ensure the view name for the given job is present in the <see cref="AzureConstants.TableNames.ViewNameDate"/> table.
        /// </summary>
        private async Task PopulateViewName(JobId jobId, DateTimeOffset buildDate)
        {
            try
            {
                var entity = new ViewNameEntity(buildDate, AzureUtil.GetViewName(jobId));
                var op = TableOperation.Insert(entity);
                await _viewNameDateTable.ExecuteAsync(op);
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 409)
            {
                // It's expected to get errors here because we're inserting duplicate data.  All that matters is the
                // data is present in the table.  
            }
        }

        /// <summary>
        /// Update the table storage to contain the result of the specified build.
        /// </summary>
        private async Task<PopulateData> GetPopulateDataCore(BoundBuildId id)
        {
            var buildInfo = await _client.GetBuildInfoAsync(id.BuildId);
            var jobKind = await _client.GetJobKindAsync(id.JobId);

            PullRequestInfo prInfo = null;
            if (JobUtil.IsPullRequestJobName(id.JobId.Name))
            {
                try
                {
                    prInfo = await _client.GetPullRequestInfoAsync(id.BuildId);
                }
                catch (Exception ex)
                {
                    // TODO: Flow builds don't have the PR directly in the triggered jobs.  Have to walk
                    // back up to the parent job.  For now swallow this error so we don't trigger false
                    // positives in the error detection.
                    _textWriter.WriteLine($"Error pulling PR info for {id}: {ex.Message}");
                }
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
                    classification = await PopulateFailedBuildResult(buildInfo, jobKind, prInfo);
                    break;
                case BuildState.Running:
                    classification = BuildResultClassification.Unknown;
                    break;
                default:
                    throw new Exception($"Invalid enum: {buildInfo.State} for {id.JobName} - {id.Number}");
            }

            var resultEntity = new BuildResultEntity(
                id,
                buildInfo.Date,
                buildInfo.Duration,
                jobKind: jobKind,
                machineName: buildInfo.MachineName,
                classification: classification,
                prInfo: prInfo);

            var failures = classification.Kind == ClassificationKind.TestFailure
                ? await GetUnitTestFailures(buildInfo, jobKind, prInfo)
                : new List<BuildFailureEntity>();

            return new PopulateData(resultEntity, failures);
        }

        private async Task<BuildResultClassification> PopulateFailedBuildResult(BuildInfo buildInfo, string jobKind, PullRequestInfo prInfo)
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

        private async Task<List<BuildFailureEntity>> GetUnitTestFailures(BuildInfo buildInfo, string jobKind, PullRequestInfo prInfo)
        {
            // TODO: Resolve this with CoreCLR.  They are producing way too many failures at the moment though
            // and we need to stop uploading 50,000 rows a day until we can resolve this.
            if (buildInfo.Id.JobName.Contains("dotnet_coreclr"))
            {
                return new List<BuildFailureEntity>(capacity: 0);
            }

            var buildId = buildInfo.Id;
            var testCaseNames = await _client.GetFailedTestCasesAsync(buildInfo.BuildId);

            // Ignore obnoxious long test names.  This is a temporary work around due to CoreFX generating giantic test
            // names and log files.
            // https://github.com/dotnet/corefx/pull/11905
            if (testCaseNames.Any(x => x.Length > 10000))
            {
                return new List<BuildFailureEntity>(capacity: 0);
            }

            var entityList = testCaseNames
                .Select(x => BuildFailureEntity.CreateTestCaseFailure(buildInfo.Date, buildId, x, jobKind: jobKind, machineName: buildInfo.MachineName, prInfo: prInfo))
                .ToList();
            EnsureTestCaseNamesUnique(entityList);
            return entityList;
        }

        private void WriteLine(BoundBuildId buildId, string message)
        {
            _textWriter.WriteLine($"{buildId.JobName} - {buildId.Number}: {message}");
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
