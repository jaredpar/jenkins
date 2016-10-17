using Dashboard.Azure;
using Dashboard.Azure.Builds;
using Dashboard.Jenkins;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
        internal const int MissingBuildLimit = 10;

        private readonly CloudTable _buildStateTable;
        private readonly CloudTable _buildStateKeyTable;
        private readonly CloudQueue _processBuildQueue;
        private readonly CloudQueue _emailBuildQueue;
        private readonly TextWriter _logger;

        internal StateUtil(
            CloudTable buildStateTable,
            CloudTable buildStateKeyTable,
            CloudQueue processBuildQueue,
            CloudQueue emailBuildQueue,
            TextWriter logger)
        {
            Debug.Assert(buildStateTable.Name == TableNames.BuildState);
            Debug.Assert(buildStateKeyTable.Name == TableNames.BuildStateKey);
            Debug.Assert(processBuildQueue.Name == QueueNames.ProcessBuild);
            Debug.Assert(emailBuildQueue.Name == QueueNames.EmailBuild);
            _buildStateTable = buildStateTable;
            _buildStateKeyTable = buildStateKeyTable;
            _processBuildQueue = processBuildQueue;
            _emailBuildQueue = emailBuildQueue;
            _logger = logger;
        }

        internal async Task ProcessBuildEvent(BuildEventMessageJson message, CancellationToken cancellationToken)
        {
            var isBuildFinished = message.Phase == "FINALIZED";
            var key = await GetOrCreateBuildStateKey(message.BoundBuildId);
            var entityKey = BuildStateEntity.GetEntityKey(key, message.BoundBuildId);

            // Ensure there is an entry in the build state table for this build.
            var entity = await AzureUtil.QueryAsync<BuildStateEntity>(_buildStateTable, entityKey, cancellationToken);
            if (entity == null || entity.IsBuildFinished != isBuildFinished)
            {
                entity = new BuildStateEntity(key, message.BoundBuildId, isBuildFinished);
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
            task.Start();
            return await task;
        }

        /// <summary>
        /// Populate the given build and update the unprocessed table accordingly.  If there is no 
        /// existing entity in the unprocessed table, this won't add one.  It will only update existing
        /// ones.
        /// </summary>
        internal async Task Populate(BuildStateMessage message, BuildTablePopulator populator, CancellationToken cancellationToken)
        {
            var buildId = message.BuildId;
            var entityKey = BuildStateEntity.GetEntityKey(message.BuildStateKey, message.BoundBuildId);
            var entity = await AzureUtil.QueryAsync<BuildStateEntity>(_buildStateTable, entityKey, cancellationToken);

            var completed = await PopulateCore(entity, populator, cancellationToken);

            // Unable to complete the build, consider this is a 404 missing that we need to handle. 
            if (!completed && entity.BuildMissingCount > MissingBuildLimit)
            {
                completed = await PopulateMissing(entity, populator, cancellationToken);
            }

            if (completed)
            {
                return;
            }

            var isDone = (DateTimeOffset.UtcNow - entity.BuildStateKey.DateTime).TotalDays > DayWindow;
            if (isDone)
            {
                await EnqueueEmailBuild(entity.BuildStateKey, entity.BoundBuildId, cancellationToken);
            }
            else
            { 
                // Wait an hour to retry.  Hope that a bug fix is uploaded or jenkins gets back into a good state.
                await EnqueueProcessBuild(entity.BuildStateKey, entity.BoundBuildId, TimeSpan.FromHours(1), cancellationToken);
            }
        }

        internal async Task<bool> PopulateCore(BuildStateEntity entity, BuildTablePopulator populator, CancellationToken cancellationToken)
        {
            var buildId = entity.BoundBuildId;
            var key = entity.BuildStateKey;

            await CheckFinished(entity, cancellationToken);

            // Don't process the build unless it's known to have finished.
            if (!entity.IsBuildFinished)
            {
                _logger.WriteLine($"Build {buildId.JobId} isn't finished yet");
                return false;
            }

            // The build was completely populated by a previous message.  No more work needed.
            if (entity.IsDataComplete)
            {
                _logger.WriteLine($"Build {buildId.JobId} is already populated");
                return true;
            }

            try
            {
                _logger.WriteLine($"Populating {buildId.JobId} ... ");
                await populator.PopulateBuild(buildId);

                _logger.WriteLine($"Updating the build data state ..");
                entity.IsDataComplete = true;
                entity.Error = null;
                entity.ETag = "*";
                await _buildStateTable.ExecuteAsync(TableOperation.Replace(entity), cancellationToken);

                _logger.WriteLine($"Completed");
                return true;
            }
            catch (Exception e)
            {
                _logger.WriteLine($"Failed");
                _logger.WriteLine(e);

                await CheckForMissingBuild(entity, cancellationToken);

                try
                {
                    entity.Error = $"{e.Message} - {e.StackTrace.Take(1000)}";
                    await _buildStateTable.ExecuteAsync(TableOperation.Replace(entity));
                }
                catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 412)
                {
                    // It's possible the enity was updated in parallel.  That's okay.  This table
                    // is meant as an approximation of the build state and always moving towards complete.
                }

                return false;
            }
        }

        /// <summary>
        /// The build is determined to be missing.  Finish the build according to that.
        /// </summary>
        internal async Task<bool> PopulateMissing(BuildStateEntity entity, BuildTablePopulator populator, CancellationToken cancellationToken)
        {
            try
            {
                await populator.PopulateBuildMissing(entity.BoundBuildId);

                entity.IsBuildFinished = true;
                entity.IsDataComplete = true;
                entity.Error = "Build missing";

                await _buildStateTable.ExecuteAsync(TableOperation.InsertOrReplace(entity), cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                // This is frankly the best possible outcome.  This is the worst state we can have for a build
                // so any other thread giving a result can't be worse.
                _logger.WriteLine($"Error populating build {entity.BuildId} as missing {ex}");
                return false;
            }
        }

        private async Task CheckFinished(BuildStateEntity entity, CancellationToken cancellationToken)
        {
            if (entity.IsBuildFinished)
            {
                return;
            }

            try
            {
                _logger.WriteLine($"Checking to see if {entity.BuildId} has completed");
                var client = CreateJenkinsClient(entity.BoundBuildId);
                var buildInfo = await client.GetBuildInfoAsync(entity.BuildId);
                if (buildInfo.State != BuildState.Running)
                {
                    entity.IsBuildFinished = true;
                    await _buildStateTable.ExecuteAsync(TableOperation.Replace(entity), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await CheckForMissingBuild(entity, cancellationToken);
                _logger.WriteLine($"Unable to query job state {ex.Message}");
            }
        }

        /// <summary>
        /// This is called when we get an exception processing a build.  This accounts for the case that a 
        /// build is missing.  Can happen during Jenkins restart, build archiving, etc ...
        /// 
        /// This is fundamentally a heuristic.  It's interpreting 404 essentially as permanently missing vs.
        /// Jenkins is just down for a period of time.  This is understood and accounted for as best as possible.
        /// </summary>
        private async Task CheckForMissingBuild(BuildStateEntity entity, CancellationToken cancellationToken)
        {
            var isMissing = await IsBuildTemporarilyMissing(entity);
            if (!isMissing)
            {
                return;
            }

            entity.BuildMissingCount++;
            try
            {
                await _buildStateTable.ExecuteAsync(TableOperation.InsertOrReplace(entity), cancellationToken);
            }
            catch
            {
                // Possible to be updated in parallel.  Always moving to a final state so that's fine.
            }
        }

        private async Task<bool> IsBuildTemporarilyMissing(BuildStateEntity entity)
        {
            var buildId = entity.BoundBuildId;
            try
            {
                var client = new RestClient(buildId.Host);
                var request = new RestRequest(buildId.BuildUri.PathAndQuery, Method.GET);
                var response = await client.ExecuteTaskAsync(request);
                return response.StatusCode == HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                _logger.WriteLine($"Error checking for 404 on {buildId} {ex}");
                return false;
            }
        }

        internal static JenkinsClient CreateJenkinsClient(BoundBuildId buildId)
        {
            if (JobUtil.IsAuthNeededHeuristic(buildId.JobId))
            {
                var githubConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.GithubConnectionStringName);
                var host = buildId.GetHostUri(useHttps: true);
                return new JenkinsClient(host, githubConnectionString);
            }
            else
            {
                return new JenkinsClient(buildId.Host);
            }
        }

        private async Task EnqueueEmailBuild(DateTimeKey buildStateKey, BoundBuildId buildId, CancellationToken cancellationToken)
        {
            await EnqueueCore(_emailBuildQueue, buildStateKey, buildId, null, cancellationToken);
        }

        private async Task EnqueueProcessBuild(DateTimeKey buildStateKey, BoundBuildId buildId, TimeSpan? delay, CancellationToken cancellationToken)
        {
            await EnqueueCore(_processBuildQueue, buildStateKey, buildId, delay, cancellationToken);
        }

        private static async Task EnqueueCore(CloudQueue queue, DateTimeKey buildStateKey, BoundBuildId buildId, TimeSpan? delay, CancellationToken cancellationToken)
        {
            // Enqueue a message to process the build.  Insert a delay if the build isn't finished yet so that 
            // we don't unnecessarily ask Jenkins for information.
            var buildMessage = new BuildStateMessage()
            {
                BuildStateKeyRaw = buildStateKey.Key,
                BuildNumber = buildId.Number,
                HostRaw = buildId.Host.ToString(),
                JobName = buildId.JobName
            };

            var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(buildMessage));
            await queue.AddMessageAsync(
                queueMessage, 
                timeToLive: null, 
                initialVisibilityDelay: delay, 
                options: null, 
                operationContext: null, 
                cancellationToken: cancellationToken);
        }
    }
}
