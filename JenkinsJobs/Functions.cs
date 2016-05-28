using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Dashboard;
using Dashboard.Azure;
using Dashboard.Jenkins;
using SendGrid;
using System.Net.Mail;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json.Linq;

namespace Dashboard.StorageBuilder
{
    public class Functions
    {
        public static async Task BuildEvent(
            [QueueTrigger(AzureConstants.QueueNames.BuildEvent)] string message,
            [Queue(AzureConstants.QueueNames.ProcessBuild)] CloudQueue processBuildQueue,
            [Table(AzureConstants.TableNames.UnprocessedBuild)] CloudTable unprocessedBuildTable,
            TextWriter logger,
            CancellationToken cancellationToken)
        {
            var messageJson = (BuildEventMessageJson)JsonConvert.DeserializeObject(message, typeof(BuildEventMessageJson));

            // First make sure that we note this value in the unprocessed table as it has not yet
            // been processed.
            var entity = new UnprocessedBuildEntity(messageJson.BuildId);
            var operation = TableOperation.InsertOrReplace(entity);
            await unprocessedBuildTable.ExecuteAsync(TableOperation.InsertOrReplace(entity));

            // If this is a finalized event then the build is ready.  Go ahead and process it now.
            if (messageJson.Phase == "FINALIZED")
            {
                logger.WriteLine($"Queue event to process build {messageJson.BuildId}");
                await StateUtil.EnqueueProcessBuild(processBuildQueue, messageJson.JenkinsHostName, messageJson.BuildId);
            }
        }

        /// <summary>
        /// Populate the build table by processing the given message.  This function doesn't handle
        /// any build state semantics.  Instead it just processes the build and updates the build 
        /// result tables.
        /// </summary>
        public static async Task PopulateBuildData(
            [QueueTrigger(AzureConstants.QueueNames.ProcessBuild)] string message,
            [Table(AzureConstants.TableNames.BuildResultDate)] CloudTable buildResultDateTable,
            [Table(AzureConstants.TableNames.BuildResultExact)] CloudTable buildResultExactTable,
            [Table(AzureConstants.TableNames.BuildFailureDate)] CloudTable buildFailureDateTable,
            [Table(AzureConstants.TableNames.BuildFailureExact)] CloudTable buildFailureExactTable,
            TextWriter logger,
            CancellationToken cancellationToken)
        {
            var buildIdJson = (BuildIdJson)JsonConvert.DeserializeObject(message, typeof(BuildIdJson));
            var client = StateUtil.CreateJenkinsClient(buildIdJson.JenkinsUrl, buildIdJson.JobId);
            var populator = new BuildTablePopulator(
                buildResultDateTable: buildResultDateTable,
                buildResultExactTable: buildResultExactTable,
                buildFailureDateTable: buildFailureDateTable,
                buildFailureExactTable: buildFailureExactTable,
                client: client,
                textWriter: logger);
            await populator.PopulateBuild(buildIdJson.BuildId);
        }

        /// <summary>
        /// Update the jobs in the unprocessed table.
        /// </summary>
        public static async Task UpdateUnprocessedTable(
            [TimerTrigger("0 0/30 * * * *", RunOnStartup = true)] TimerInfo timerInfo,
            [QueueTrigger(AzureConstants.QueueNames.ProcessBuild)] CloudQueue processBuildQueue,
            [Table(AzureConstants.TableNames.UnprocessedBuild)] CloudTable unprocessedBuildTable,
            [Table(AzureConstants.TableNames.BuildResultExact)] CloudTable buildResultExactTable,
            TextWriter logger,
            CancellationToken cancellationToken)
        {
            var util = new StateUtil(
                unprocessedBuildTable: unprocessedBuildTable,
                buildResultExact: buildResultExactTable,
                processBuildQueue: processBuildQueue,
                logger: logger);
            await util.Update(cancellationToken);
        }

        public static async Task CleanTestCache(
            [TimerTrigger("0 0 * * * *", RunOnStartup = true)] TimerInfo timerInfo,
            [Blob(AzureConstants.ContainerNames.TestResults)] IEnumerable<ICloudBlob> blobs,
            TextWriter logger,
            CancellationToken cancellationToken)
        {
            var limit = DateTimeOffset.UtcNow - TimeSpan.FromDays(10);
            var query = blobs.Where(x => x.Properties.LastModified < limit).ToList();
            logger.WriteLine($"Deleting {query.Count} stale test results");

            var count = 0;
            foreach (var blob in query)
            {
                await blob.DeleteIfExistsAsync();
                count++;
                if (count % 10 == 0)
                {
                    Console.WriteLine($"  deleted {count}");
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            logger.WriteLine($"Completed deleting");
        }

        /*
        private static async Task SendEmail(string text)
        {
            var message = new SendGridMessage();
            message.AddTo("jaredpparsons@gmail.com");
            message.AddTo("jaredpar@microsoft.com");
            message.From = new MailAddress("jaredpar@jdash.azurewebsites.net");
            message.Subject = "Jenkins Storage Populate Errors";
            message.Text = text;

            var key = CloudConfigurationManager.GetSetting(SharedConstants.SendGridApiKeySettingName);
            var web = new Web(apiKey: key);
            await web.DeliverAsync(message).ConfigureAwait(false);
        }

        private static string BuildMessage(List<BuildAnalyzeError> buildAnalyzeErrors)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Total Errors: {buildAnalyzeErrors.Count}");
            foreach (var error in buildAnalyzeErrors)
            {
                builder.AppendLine($"Build: {error.BuildId.JobName} - {error.BuildId.Number}");
                builder.AppendLine($"Url: {JenkinsUtil.GetUri(SharedConstants.DotnetJenkinsUri, error.BuildId)}");
                builder.AppendLine($"Message: {error.Exception.Message}");
                builder.AppendLine($"Stack: {error.Exception.StackTrace}");
                builder.AppendLine("");
            }

            return builder.ToString();
        }
        */
    }
}
