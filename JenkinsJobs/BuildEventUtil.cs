using Dashboard.Azure;
using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard.StorageBuilder
{
    /// <summary>
    /// Process build events and populate the associated tables.  Populating a build is idempotent.  Hence all of the 
    /// logic around when to process and how to recover from error favors just re-populating the same build.
    /// </summary>
    internal sealed class BuildEventUtil
    {
        private readonly CloudTable _buildEventTable;
        private readonly CloudTable _buildProcessedTable;
        private readonly CloudTable _buildFailureTable;
        private readonly CloudTable _buildResultTable;
        private readonly TextWriter _textWriter;
        private readonly string _githubConnectionString;

        internal BuildEventUtil(CloudTable buildEventTable, CloudTable buildProcessedTable, CloudTable buildFailureTable, CloudTable buildResultTable, TextWriter textWriter, string githubConnectionString)
        {
            _buildEventTable = buildEventTable;
            _buildProcessedTable = buildProcessedTable;
            _buildFailureTable = buildFailureTable;
            _buildResultTable = buildResultTable;
            _githubConnectionString = githubConnectionString;
            _textWriter = textWriter;
        }

        internal async Task<List<BuildAnalyzeError>> ProcessCompleted(BuildEventMessageJson message, CancellationToken cancellationToken)
        {
            var jenkinsUri = new Uri($"https://{message.JenkinsHostName}");
            var jenkinsClient = new JenkinsClient(jenkinsUri, connectionString: _githubConnectionString);
            var jobTableUtil = new BuildTableUtil(buildProcessedTable: _buildProcessedTable, buildFailureTable: _buildFailureTable, buildResultTable: _buildResultTable, client: jenkinsClient, textWriter: _textWriter);

            try
            {
                await jobTableUtil.PopulateBuild(message.BuildId);
            }
            catch
            {
                // In the case there is an error populating the build we should make sure the 
                // BuildEvent table contains an entry for this build.  This way later tasks can
                // come along and reprocess this query later on when the error is corrected.
                await EnsureBuildEventEntity(message, cancellationToken);
                throw;
            }

            // Now that the build is processed delete the entity from the event table to signify that it no longer needs
            // to be processed.
            try
            {
                var entity = GetBuildEntity(message.BuildId);
                if (entity != null)
                {
                    await _buildEventTable.ExecuteAsync(TableOperation.Delete(entity), cancellationToken);
                }
            }
            catch (StorageException)
            {
                // A parallel operation has already completed this job.  This is okay.
            }

            return jobTableUtil.BuildAnalyzeErrors.ToList();
        }

        private BuildEventEntity GetOrCreateBuildEventEntity(BuildEventMessageJson message)
        {
            var entity = GetBuildEntity(message.BuildId);
            if (entity != null)
            {
                return entity;
            }

            return CreateBuildEventEntity(message);
        }

        private BuildEventEntity GetBuildEntity(BuildId buildId)
        {
            var key = BuildEventEntity.GetEntityKey(buildId);
            // TODO Async? 
            return AzureUtil.QueryTable<BuildEventEntity>(_buildEventTable, key);
        }

        private BuildEventEntity CreateBuildEventEntity(BuildEventMessageJson message)
        {
            var jobId = JenkinsUtil.ConvertPathToJobId(message.JobName);
            var uri = new Uri(message.Url);
            return new BuildEventEntity(new BuildId(message.Number, jobId))
            {
                JenkinsHostName = message.JenkinsHostName,
                QueueId = message.QueueId,
                Status = message.Status,
                Phase = message.Phase,
            };
        }

        private async Task EnsureBuildEventEntity(BuildEventMessageJson message, CancellationToken cancellationToken)
        {
            try
            {
                var entity = CreateBuildEventEntity(message);
                await _buildEventTable.ExecuteAsync(TableOperation.Insert(entity), cancellationToken);
            }
            catch (StorageException)
            {
                // Entity already inserted.  Mission accomplished.
            }
        }

        private static bool IsPhaseCompleted(string phase) => phase == "FINALIZED";
    }
}

