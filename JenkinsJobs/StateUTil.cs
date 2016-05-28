using Dashboard.Azure;
using Dashboard.Jenkins;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage.Queue;
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
    internal class StateUtil
    {
        private readonly CloudTable _unprocessedBuildTable;
        private readonly CloudTable _buildResultExact;
        private readonly CloudQueue _processBuildQueue;
        private readonly TextWriter _logger;

        internal StateUtil(
            CloudTable unprocessedBuildTable, 
            CloudTable buildResultExact,
            CloudQueue processBuildQueue,
            TextWriter logger)
        {
            _unprocessedBuildTable = unprocessedBuildTable;
            _buildResultExact = buildResultExact;
            _processBuildQueue = processBuildQueue;
            _logger = logger;
        }

        internal async Task Update(CancellationToken cancellationToken)
        {
            var query = new TableQuery<UnprocessedBuildEntity>();
            await AzureUtil.QueryAsync(
                _unprocessedBuildTable, 
                query, 
                e => UpdateEntity(e, cancellationToken),
                cancellationToken);
        }

        private async Task UpdateEntity(UnprocessedBuildEntity entity, CancellationToken cancellationToken)
        {
            if (await HasPopulatedData(entity.BuildId, cancellationToken))
            {
                _logger.WriteLine($"Build populated {entity.BuildId}");
                try
                {
                    var operation = TableOperation.Delete(entity);
                    await _unprocessedBuildTable.ExecuteAsync(operation);
                }
                catch
                {
                    // It's okay if another task deletes this in parallel.
                }

                return;
            }

            // TODO: Need to store the Jenkins URI in the UnprocessedBuildEntity
            var jenkinsUri = SharedConstants.DotnetJenkinsUri;
            var client = CreateJenkinsClient(jenkinsUri, entity.JobId);
            var buildInfo = await client.GetBuildInfoAsync(entity.BuildId);
            if (buildInfo.State != BuildState.Running)
            {
                _logger.WriteLine($"Build complete and setting up processing {entity.BuildId}");
                await EnqueueProcessBuild(_processBuildQueue, jenkinsUri.Host, entity.BuildId);
            }
        }

        /// <summary>
        /// Has this entity been completely processed at this point. 
        /// </summary>
        private async Task<bool> HasPopulatedData(BuildId buildId, CancellationToken cancellationToken)
        {
            var key = BuildResultEntity.GetExactEntityKey(buildId);
            var entity = await AzureUtil.QueryAsync<BuildResultEntity>(_buildResultExact, key, cancellationToken);
            return entity != null;
        }

        internal static JenkinsClient CreateJenkinsClient(Uri jenkinsUrl, JobId jobId)
        {
            // TODO: don't authenticate when it's not https
            // TODO: Bit of a hack.  Avoiding API rate limit issues by using a hueristic of 
            // when to do authentication.
            if (jobId.Name.Contains("Private"))
            {
                var githubConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.GithubConnectionStringName);
                return new JenkinsClient(jenkinsUrl, githubConnectionString);
            }
            else
            {
                return new JenkinsClient(jenkinsUrl);
            }
        }


        internal static async Task EnqueueProcessBuild(CloudQueue processBuildQueue, string jenkinsHostName, BuildId buildId)
        {
            var buildIdJson = new BuildIdJson()
            {
                JenkinsHostName = jenkinsHostName,
                BuildNumber = buildId.Number,
                JobName = buildId.JobName
            };

            var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(buildIdJson));
            await processBuildQueue.AddMessageAsync(queueMessage);
        }
    }
}
