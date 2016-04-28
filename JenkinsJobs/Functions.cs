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

namespace Dashboard.StorageBuilder
{
    public class Functions
    {
        public static void BuildEvent(
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
            if (messageJson.Phase == "COMPLETED")
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
                populator.PopulateBuild(messageJson.BuildId).Wait();
            }
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
