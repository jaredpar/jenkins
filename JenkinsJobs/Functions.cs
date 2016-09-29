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
using Dashboard.Azure.Json;
using Dashboard.Jenkins;
using SendGrid;
using System.Net.Mail;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json.Linq;
using static Dashboard.Azure.AzureConstants;
using System.Net;

namespace Dashboard.StorageBuilder
{
    public class Functions
    {
        public static async Task BuildEvent(
            [QueueTrigger(QueueNames.BuildEvent)] string message,
            [Queue(QueueNames.ProcessBuild)] CloudQueue processBuildQueue,
            [Queue(QueueNames.EmailBuild)] CloudQueue emailBuildQueue,
            [Table(TableNames.BuildState)] CloudTable buildStateTable,
            [Table(TableNames.BuildStateKey)] CloudTable buildStateKeyTable,
            TextWriter logger,
            CancellationToken cancellationToken)
        {
            var messageJson = JsonConvert.DeserializeObject<BuildEventMessageJson>(message);
            var stateUtil = new StateUtil(
                buildStateTable,
                buildStateKeyTable,
                processBuildQueue,
                emailBuildQueue,
                logger);
            await stateUtil.ProcessBuildEvent(messageJson, cancellationToken);
        }

        /// <summary>
        /// Populate the build table by processing the given message.  This function doesn't handle
        /// any build state semantics.  Instead it just processes the build and updates the build 
        /// result tables.
        /// </summary>
        public static async Task PopulateBuildData(
            [QueueTrigger(QueueNames.ProcessBuild)] string message,
            [Table(TableNames.BuildState)] CloudTable buildStateTable,
            [Table(TableNames.BuildStateKey)] CloudTable buildStateKeyTable,
            [Table(TableNames.BuildResultDate)] CloudTable buildResultDateTable,
            [Table(TableNames.BuildResultExact)] CloudTable buildResultExactTable,
            [Table(TableNames.BuildFailureDate)] CloudTable buildFailureDateTable,
            [Table(TableNames.BuildFailureExact)] CloudTable buildFailureExactTable,
            [Table(TableNames.ViewNameDate)] CloudTable viewNameDateTable,
            [Queue(QueueNames.ProcessBuild)] CloudQueue processBuildQueue,
            [Queue(QueueNames.EmailBuild)] CloudQueue emailBuildQueue,
            TextWriter logger,
            CancellationToken cancellationToken)
        {
            var buildIdJson = (BuildStateMessage)JsonConvert.DeserializeObject(message, typeof(BuildStateMessage));

            var client = StateUtil.CreateJenkinsClient(buildIdJson.HostName, buildIdJson.JobId);
            var populator = new BuildTablePopulator(
                buildResultDateTable: buildResultDateTable,
                buildResultExactTable: buildResultExactTable,
                buildFailureDateTable: buildFailureDateTable,
                buildFailureExactTable: buildFailureExactTable,
                viewNameDateTable: viewNameDateTable,
                client: client,
                textWriter: logger);
            var stateUtil = new StateUtil(
                buildStateKeyTable: buildStateKeyTable,
                buildStateTable: buildStateTable,
                processBuildQueue: processBuildQueue,
                emailBuildQueue: emailBuildQueue,
                logger: logger);
            await stateUtil.Populate(buildIdJson, populator, force: false, cancellationToken: cancellationToken);
        }

        public static async Task EmailFailedBuild(
            [Queue(QueueNames.EmailBuild)] string messageJson,
            [Table(TableNames.BuildState)] CloudTable buildStateTable,
            TextWriter logger,
            CancellationToken cancellationToken)
        {
            var buildMessage = JsonConvert.DeserializeObject<BuildStateMessage>(messageJson);
            var entityKey = BuildStateEntity.GetEntityKey(buildMessage.BuildStateKey, buildMessage.BoundBuildId);
            var entity = await AzureUtil.QueryAsync<BuildStateEntity>(buildStateTable, entityKey, cancellationToken);
            var textBuilder = new StringBuilder();
            var htmlBuilder = new StringBuilder();
            AppendEmailText(entity, textBuilder, htmlBuilder);

            var message = new SendGridMessage()
            {
                Html = htmlBuilder.ToString(),
                Text = textBuilder.ToString(),
            };

            message.AddTo("jaredpparsons@gmail.com");
            message.AddTo("jaredpar@microsoft.com");
            message.From = new MailAddress("jaredpar@jdash.azurewebsites.net");
            message.Subject = $"Build process error {entity.JobName}";

            var key = CloudConfigurationManager.GetSetting(SharedConstants.SendGridApiKeySettingName);
            var web = new Web(apiKey: key);
            await web.DeliverAsync(message);
        }

        private static void AppendEmailText(BuildStateEntity entity, StringBuilder textBuilder, StringBuilder htmlBuilder)
        {
            var boundBuildId = entity.BoundBuildId;
            var buildId = boundBuildId.BuildId;

            textBuilder.Append($"Failed to process build: {boundBuildId.GetBuildUri(useHttps: false)}");
            textBuilder.Append($"Error: {entity.Error}");

            htmlBuilder.Append($@"<div>");
            htmlBuilder.Append($@"<div>Build <a href=""{boundBuildId.GetBuildUri(useHttps: false)}"">{buildId.JobName} {buildId.Number}</a></div>");
            htmlBuilder.Append($@"<div>Error: {WebUtility.HtmlEncode(entity.Error)}</div>");
            htmlBuilder.Append($@"</div>");
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
    }
}
