using Dashboard.Azure;
using Dashboard.Jenkins;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SendGrid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Dashboard.Azure.AzureConstants;

namespace Dashboard.StorageBuilder
{
    internal sealed class StateUtil
    {
        /// <summary>
        /// The number of days in which the tool will track an individual job.  After that if the job cannot be contacted
        /// anymore or processed completely it will be considered in error and no longer tracked.
        /// </summary>
        internal const int DayWindow = 3;

        private readonly CloudTable _buildStateTable;
        private readonly CloudTable _buildStateKeyTable;
        private readonly CloudQueue _processBuildQueue;
        private readonly TextWriter _logger;

        internal StateUtil(
            CloudTable buildStateTable,
            CloudTable buildStateKeyTable,
            CloudQueue processBuildQueue,
            TextWriter logger)
        {
            Debug.Assert(buildStateTable.Name == TableNames.BuildState);
            Debug.Assert(buildStateKeyTable.Name == TableNames.BuildStateKey);
            Debug.Assert(processBuildQueue.Name == QueueNames.ProcessBuild);
            _buildStateTable = buildStateTable;
            _buildStateKeyTable = buildStateKeyTable;
            _processBuildQueue = processBuildQueue;
            _logger = logger;
        }

        internal async Task ProcessBuildEvent(BuildEventMessageJson message, CancellationToken cancellationToken)
        {
            var isBuildFinished = message.Phase == "FINALIZED";
            var key = await GetOrCreateBuildStateKey(message.BoundBuildId);
            var entityKey = BuildStateEnity.GetEntityKey(key, message.BoundBuildId);

            // Ensure there is an entry in the build state table for this build.
            var entity = await AzureUtil.QueryAsync<BuildStateEnity>(_buildStateTable, entityKey, cancellationToken);
            if (entity == null || entity.IsBuildFinished != isBuildFinished)
            {
                entity = new BuildStateEnity(key, message.BoundBuildId, isBuildFinished);
                await _buildStateTable.ExecuteAsync(TableOperation.InsertOrReplace(entity), cancellationToken);
            }

            // Enqueue a message to process the build.  Insert a delay if the build isn't finished yet so that 
            // we don't unnecessarily ask Jenkins for information.
            var delay = isBuildFinished ? (TimeSpan?)null : TimeSpan.FromMinutes(30);
            await EnqueueProcessBuild(key, message.BoundBuildId, delay, cancellationToken);
        }

        /// <summary>
        /// Get or create the build state partition key for the build id.
        /// </summary>
        /// <remarks>
        /// This has to take into account that builds take place across days.  Which day wins doesn't matter
        /// so long as we don't duplicate the data.
        /// </remarks>
        internal async Task<DateTimeKey> GetOrCreateBuildStateKey(BoundBuildId buildId)
        {
            // TODO: do this correctly
            var key = new DateTimeKey(DateTimeOffset.UtcNow, DateTimeKeyFlags.Date);
            var task = new Task<DateTimeKey>(() => key);
            return await task;
        }

        /// <summary>
        /// Populate the given build and update the unprocessed table accordingly.  If there is no 
        /// existing entity in the unprocessed table, this won't add one.  It will only update existing
        /// ones.
        /// </summary>
        internal async Task Populate(ProcessBuildMessage message, BuildTablePopulator populator, bool force, CancellationToken cancellationToken)
        {
            var buildId = message.BuildId;
            var key = message.BuildStateKey;
            var entityKey = BuildStateEnity.GetEntityKey(key, message.BoundBuildId);
            var entity = await AzureUtil.QueryAsync<BuildStateEnity>(_buildStateTable, entityKey, cancellationToken);

            await CheckFinished(entity, cancellationToken);

            // If we are not forcing the update then check for the existence of a completed run before
            // requerying Jenkins.
            if (!force && entity.IsDataComplete)
            {
                _logger.WriteLine($"Build {buildId.JobId} is already populated");
                return;
            }

            try
            {
                _logger.Write($"Populating {buildId.JobId} ... ");
                await populator.PopulateBuild(buildId);

                _logger.Write($"Updating the build data state ..");
                entity.IsDataComplete = true;
                await _buildStateTable.ExecuteAsync(TableOperation.Replace(entity), cancellationToken);

                _logger.WriteLine($"Completed");
            }
            catch (Exception e)
            {
                _logger.WriteLine($"Failed");
                _logger.WriteLine(e);

                try
                {
                    entity.Error = $"{e.Message} - {e.StackTrace.Take(1000)}";
                    await _buildStateTable.ExecuteAsync(TableOperation.Replace(entity));
                }
                catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 409)
                {
                    // It's possible the enity was updated in parallel.  That's okay.  This table
                    // is meant as an approximation of the build state and always moving towards complete.
                }

                var isDone = (DateTimeOffset.UtcNow - key.DateTime).TotalDays > DayWindow;
                if (isDone)
                {
                    // TODO: Need to send an email here.  Or at least put a messsage in the queue to send one.
                }
                else
                { 
                    // Wait an hour to retry.  Hope that a bug fix is uploaded or jenkins gets back into a good state.
                    await EnqueueProcessBuild(message.BuildStateKey, message.BoundBuildId, TimeSpan.FromHours(1), cancellationToken);
                }
            }
        }

        private async Task CheckFinished(BuildStateEnity entity, CancellationToken cancellationToken)
        {
            if (entity.IsBuildFinished)
            {
                return;
            }

            try
            {
                _logger.WriteLine($"Checking to see if {entity.BuildId} has completed");
                var client = CreateJenkinsClient(entity.HostName, entity.JobId);
                var buildInfo = await client.GetBuildInfoAsync(entity.BuildId);
                if (buildInfo.State != BuildState.Running)
                {
                    entity.IsBuildFinished = true;
                    await _buildStateTable.ExecuteAsync(TableOperation.Replace(entity), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLine($"Unable to query job state {ex.Message}");
            }
        }

        /*
        internal async Task<SendGridMessage> Clean(int window = DayWindow, CancellationToken cancellationToken = default(CancellationToken))
        {
            var key = BuildStateEnity.GetPartitionKey(DateTimeOffset.UtcNow.AddDays(-window));
            var filter =
                FilterUtil.Combine(
                    FilterUtil.PartitionKey(key),
                    CombineOperator.And,
                    FilterUtil.Column(nameof(BuildStateEnity.IsTracked), true));
            var query = new TableQuery<BuildStateEnity>().Where(filter);
            var list = await AzureUtil.QueryAsync<BuildStateEnity>(_buildStateTable, query, cancellationToken);
            if (list.Count == 0)
            {
                return null;
            }

            var textBuilder = new StringBuilder();
            var htmlBuilder = new StringBuilder();

            foreach (var entity in list)
            {
                var boundBuildId = entity.BoundBuildID;
                var buildId = boundBuildId.BuildId;

                _logger.WriteLine($"Deleting stale data {boundBuildId.GetBuildUri(useHttps: false)}");

                textBuilder.Append($"Deleting stale data: {boundBuildId.GetBuildUri(useHttps: false)}");
                textBuilder.Append($"Eror: {entity.Error}");

                htmlBuilder.Append($@"<div>");
                htmlBuilder.Append($@"<div>Build <a href=""{boundBuildId.GetBuildUri(useHttps: false)}"">{buildId.JobName} {buildId.Number}</a></div>");
                htmlBuilder.Append($@"<div>Error: {WebUtility.HtmlEncode(entity.Error)}</div>");
                htmlBuilder.Append($@"</div>");

                entity.IsTracked = false;
            }

            await AzureUtil.InsertBatch(_unprocessedBuildTable, list);

            return new SendGridMessage()
            {
                Text = textBuilder.ToString(),
                Html = htmlBuilder.ToString()
            };
        }
        */

        internal static JenkinsClient CreateJenkinsClient(string jenkinsHostName, JobId jobId)
        {
            var builder = new UriBuilder();
            builder.Host = jenkinsHostName;

            if (JobUtil.IsAuthNeededHeuristic(jobId))
            {
                var githubConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.GithubConnectionStringName);
                builder.Scheme = Uri.UriSchemeHttps;
                return new JenkinsClient(builder.Uri, githubConnectionString);
            }
            else
            {
                builder.Scheme = Uri.UriSchemeHttp;
                return new JenkinsClient(builder.Uri);
            }
        }

        internal async Task EnqueueProcessBuild(DateTimeKey buildStateKey, BoundBuildId buildId, TimeSpan? delay, CancellationToken cancellationToken)
        {
            // Enqueue a message to process the build.  Insert a delay if the build isn't finished yet so that 
            // we don't unnecessarily ask Jenkins for information.
            var buildMessage = new ProcessBuildMessage()
            {
                BuildStateKeyRaw = buildStateKey.Key,
                BuildNumber = buildId.Number,
                HostName = buildId.HostName,
                JobName = buildId.JobName
            };

            var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(buildMessage));
            await _processBuildQueue.AddMessageAsync(
                queueMessage, 
                timeToLive: null, 
                initialVisibilityDelay: delay, 
                options: null, 
                operationContext: null, 
                cancellationToken: cancellationToken);
        }
    }
}
