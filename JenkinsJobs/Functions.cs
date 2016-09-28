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
using static Dashboard.Azure.AzureConstants;

namespace Dashboard.StorageBuilder
{
    public class Functions
    {
        public static async Task BuildEvent(
            [QueueTrigger(QueueNames.BuildEvent)] string message,
            [Queue(QueueNames.ProcessBuild)] CloudQueue processBuildQueue,
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
            TextWriter logger,
            CancellationToken cancellationToken)
        {
            var buildIdJson = (ProcessBuildMessage)JsonConvert.DeserializeObject(message, typeof(ProcessBuildMessage));

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
                logger: logger);
            await stateUtil.Populate(buildIdJson, populator, force: false, cancellationToken: cancellationToken);
        }

        /*
        /// <summary>
        /// Update the jobs in the unprocessed table.
        /// </summary>
        public static async Task UpdateUnprocessedTable(
            [TimerTrigger("0 0/30 * * * *", RunOnStartup = true)] TimerInfo timerInfo,
            [Queue(AzureConstants.QueueNames.ProcessBuild)] CloudQueue processBuildQueue,
            [Table(AzureConstants.TableNames.UnprocessedBuild)] CloudTable unprocessedBuildTable,
            [Table(AzureConstants.TableNames.BuildResultExact)] CloudTable buildResultExactTable,
            TextWriter logger,
            CancellationToken cancellationToken)
        {
            var util = new StateUtil(
                unprocessedBuildTable: unprocessedBuildTable,
                buildResultExact: buildResultExactTable,
                logger: logger);
            await util.Update(processBuildQueue, cancellationToken);
        }

        /// <summary>
        /// Clean out the old entries in the unprocessed table.
        /// </summary>
        public static async Task CleanUnprocessedTable(
            [TimerTrigger("0 0 * * * *", RunOnStartup = true)] TimerInfo timerInfo,
            [Table(AzureConstants.TableNames.UnprocessedBuild)] CloudTable unprocessedBuildTable,
            [Table(AzureConstants.TableNames.BuildResultExact)] CloudTable buildResultExactTable,
            TextWriter logger,
            CancellationToken cancellationToken)
        {
            var util = new StateUtil(
                unprocessedBuildTable: unprocessedBuildTable,
                buildResultExact: buildResultExactTable,
                logger: logger);
            var message = await util.Clean(cancellationToken);
            if (message != null)
            {
                message.AddTo("jaredpparsons@gmail.com");
                message.AddTo("jaredpar@microsoft.com");
                message.From = new MailAddress("jaredpar@jdash.azurewebsites.net");
                message.Subject = "Jenkins Build Populate Errors";

                var key = CloudConfigurationManager.GetSetting(SharedConstants.SendGridApiKeySettingName);
                var web = new Web(apiKey: key);
                await web.DeliverAsync(message).ConfigureAwait(false);
            }
        }
        */

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
