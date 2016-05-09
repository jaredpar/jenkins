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

namespace Dashboard.StorageBuilder
{
    public class Functions
    {
        public static async Task BuildEvent(
            [QueueTrigger(AzureConstants.QueueNames.BuildEvent)] string message,
            [Table(AzureConstants.TableNames.BuildResultDate)] CloudTable buildResultDateTable,
            [Table(AzureConstants.TableNames.BuildResultExact)] CloudTable buildResultExactTable,
            [Table(AzureConstants.TableNames.BuildFailureDate)] CloudTable buildFailureDateTable,
            [Table(AzureConstants.TableNames.BuildFailureExact)] CloudTable buildFailureExactTable,
            TextWriter logger,
            CancellationToken cancellationToken)
        {
            var githubConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.GithubConnectionStringName);
            var messageJson = (BuildEventMessageJson)JsonConvert.DeserializeObject(message, typeof(BuildEventMessageJson));
            if (messageJson.Phase == "COMPLETED" || messageJson.Phase == "FINALIZED")
            {
                var client = new JenkinsClient(
                    new Uri($"https://{messageJson.JenkinsHostName}"),
                    githubConnectionString);
                var populator = new BuildTablePopulator(
                    buildResultDateTable: buildResultDateTable,
                    buildResultExactTable: buildResultExactTable,
                    buildFailureDateTable: buildFailureDateTable,
                    buildFailureExactTable: buildFailureExactTable,
                    client: client,
                    textWriter: logger);
                await populator.PopulateBuild(messageJson.BuildId);
            }
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
                builder.AppendLine($"Build: {error.BuildId.JobName} - {error.BuildId.Id}");
                builder.AppendLine($"Url: {JenkinsUtil.GetUri(SharedConstants.DotnetJenkinsUri, error.BuildId)}");
                builder.AppendLine($"Message: {error.Exception.Message}");
                builder.AppendLine($"Stack: {error.Exception.StackTrace}");
                builder.AppendLine("");
            }

            return builder.ToString();
        }
    }
}
