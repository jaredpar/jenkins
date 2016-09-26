using Dashboard.Azure;
using Dashboard.Jenkins;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SendGrid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard.StorageBuilder
{
    internal sealed class StateUtil
    {
        private readonly CloudTable _unprocessedBuildTable;
        private readonly CloudTable _buildResultExact;
        private readonly TextWriter _logger;

        internal StateUtil(
            CloudTable unprocessedBuildTable, 
            CloudTable buildResultExact,
            TextWriter logger)
        {
            _unprocessedBuildTable = unprocessedBuildTable;
            _buildResultExact = buildResultExact;
            _logger = logger;
        }

        /// <summary>
        /// Populate the given build and update the unprocessed table accordingly.  If there is no 
        /// existing entity in the unprocessed table, this won't add one.  It will only update existing
        /// ones.
        /// </summary>
        internal async Task Populate(BuildId buildId, BuildTablePopulator populator, bool force, CancellationToken cancellationToken)
        {
            var key = UnprocessedBuildEntity.GetEntityKey(buildId);

            // If we are not forcing the update then check for the existence of a completed run before
            // requerying Jenkins.
            if (!force && await populator.IsPopulated(buildId))
            {
                _logger.WriteLine($"Build {buildId.JobId} is already populated");
                return;
            }

            try
            {
                _logger.Write($"Populating {buildId.JobId} ... ");
                await populator.PopulateBuild(buildId);
                await AzureUtil.MaybeDeleteAsync(_unprocessedBuildTable, key, cancellationToken);
                _logger.WriteLine($"Completed");
            }
            catch (Exception e)
            {
                _logger.WriteLine($"Failed");
                _logger.WriteLine(e);

                // Update the error state for the row.
                var entity = await AzureUtil.QueryAsync<UnprocessedBuildEntity>(_unprocessedBuildTable, key, cancellationToken);
                if (entity != null)
                {
                    entity.StatusText = $"{e.Message} - {e.StackTrace.Take(1000)}";
                    var operation = TableOperation.Replace(entity);
                    try
                    {
                        await _unprocessedBuildTable.ExecuteAsync(operation);
                    }
                    catch
                    {
                        // It's possible the enity was deleted / updated in parallel.  That's okay.  This table
                        // is meant as an approximation of the build state and always moving towards complete.
                    }
                }
            }
        }

        internal async Task<SendGridMessage> Clean(CancellationToken cancellationToken)
        {
            var limit = DateTimeOffset.UtcNow - TimeSpan.FromHours(12);
            var filter = FilterUtil.Column(
                nameof(UnprocessedBuildEntity.LastUpdate),
                limit,
                ColumnOperator.LessThanOrEqual);
            var query = new TableQuery<UnprocessedBuildEntity>().Where(filter);
            var list = await AzureUtil.QueryAsync(_unprocessedBuildTable, query, cancellationToken);
            if (list.Count == 0)
            {
                return null;
            }

            var textBuilder = new StringBuilder();
            var htmlBuilder = new StringBuilder();

            foreach (var entity in list)
            {
                var boundBuildId = entity.BoundBuildId;
                var buildId = boundBuildId.BuildId;

                // GC Stress jobs can correctly execute for up to 3 days.  This is a bit of an outlier but one we 
                // need to handle;
                if (JobUtil.IsGCStressJob(buildId.JobId))
                {
                    var stressLimit = DateTimeOffset.UtcNow - TimeSpan.FromDays(3);
                    if (entity.LastUpdate >= stressLimit)
                    {
                        continue;
                    }
                }

                _logger.WriteLine($"Deleting stale data {boundBuildId.Uri}");

                textBuilder.Append($"Deleting stale data: {boundBuildId.Uri}");
                textBuilder.Append($"Eror: {entity.StatusText}");

                htmlBuilder.Append($@"<div>");
                htmlBuilder.Append($@"<div>Build <a href=""{boundBuildId.Uri}"">{buildId.JobName} {buildId.Number}</a></div>");
                htmlBuilder.Append($@"<div>Error: {WebUtility.HtmlEncode(entity.StatusText)}</div>");
                htmlBuilder.Append($@"</div>");
            }

            await AzureUtil.DeleteBatchUnordered(_unprocessedBuildTable, list);

            return new SendGridMessage()
            {
                Text = textBuilder.ToString(),
                Html = htmlBuilder.ToString()
            };
        }

        internal async Task Update(CloudQueue processBuildQueue, CancellationToken cancellationToken)
        {
            var query = new TableQuery<UnprocessedBuildEntity>();
            await AzureUtil.QueryAsync(
                _unprocessedBuildTable, 
                query, 
                e => UpdateEntity(e, processBuildQueue, cancellationToken),
                cancellationToken);
        }

        private async Task UpdateEntity(UnprocessedBuildEntity entity, CloudQueue processBuildQueue, CancellationToken cancellationToken)
        {
            var jenkinsUri = entity.BoundBuildId.HostUri;
            var buildId = entity.BuildId;
            var client = CreateJenkinsClient(jenkinsUri.Host, entity.JobId);
            try
            {
                var buildInfo = await client.GetBuildInfoAsync(buildId);
                if (buildInfo.State == BuildState.Running)
                {
                    _logger.WriteLine($"Build {buildId}: stil running");
                    if (string.IsNullOrEmpty(entity.StatusText))
                    {
                        entity.StatusText = "Still running";
                        try
                        {
                            await _unprocessedBuildTable.ExecuteAsync(TableOperation.Replace(entity));
                        }
                        catch
                        {
                            // Can be deleted in parallel.  That's okay and should be ignored.
                        }
                    }
                }
                else
                {
                    _logger.WriteLine($"Build {buildId}: sending for processing as it's completed");
                    await EnqueueProcessBuild(processBuildQueue, jenkinsUri.Host, buildId);
                }
            }
            catch (Exception e)
            {
                _logger.WriteLine($"Build {buildId}: error querying Jenkins: {e}");
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
