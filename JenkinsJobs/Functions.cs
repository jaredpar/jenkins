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
        [Singleton]
        public static async Task BuildEvent(
            [QueueTrigger(AzureConstants.QueueNames.BuildEvent)] string message,
            [Table(BuildEventEntity.TableName)] CloudTable buildEventTable,
            [Table(BuildProcessedEntity.TableName)] CloudTable buildProcessedTable,
            [Table(BuildFailureEntity.TableName)] CloudTable buildFailureTable,
            [Table(BuildResultEntity.TableName)] CloudTable buildResultTable,
            TextWriter logger,
            CancellationToken cancellationToken)
        {
            var githubConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.GithubConnectionStringName);
            var util = new BuildEventUtil(
                buildEventTable: buildEventTable, 
                buildProcessedTable: buildProcessedTable, 
                buildFailureTable: buildFailureTable, 
                buildResultTable: buildResultTable,
                textWriter: logger,
                githubConnectionString: githubConnectionString);
            var messageJson = (MessageJson)JsonConvert.DeserializeObject(message, typeof(MessageJson));
            var list = await util.Process(messageJson, cancellationToken);
            if (list.Count > 0)
            {
                await SendEmail(BuildMessage(list));
            }
        }

        public static async Task PopulateBuildTables(TextWriter logger)
        {
            try
            {
                var list = await PopulateBuildTablesCore(logger);
                if (list.Count > 0)
                {
                    await SendEmail(BuildMessage(list));
                }
            }
            catch (Exception ex)
            {
                await SendEmail($"Overall exception: {ex.Message} {Environment.NewLine}{ex.StackTrace}");
            }
        }

        private static async Task<List<BuildAnalyzeError>> PopulateBuildTablesCore(TextWriter logger)
        {
            var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var buildFailureTable = tableClient.GetTableReference(AzureConstants.TableNames.BuildFailure);
            var buildProcessedTable = tableClient.GetTableReference(AzureConstants.TableNames.BuildProcessed);
            var buildResultTable = tableClient.GetTableReference(BuildResultEntity.TableName);

            // TODO: Need a Jenkins token as well to be able to query our non-public jobs.
            var githubConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.GithubConnectionStringName);
            var client = string.IsNullOrEmpty(githubConnectionString)
                ? new JenkinsClient(SharedConstants.DotnetJenkinsUri)
                : new JenkinsClient(SharedConstants.DotnetJenkinsUri, connectionString: githubConnectionString);

            var jobs = client.GetJobIds();
            var util = new JobTableUtil(buildProcessedTable: buildProcessedTable, buildFailureTable: buildFailureTable, buildResultTable: buildResultTable, client: client, textWriter: logger);
            await util.MoveUnknownToIgnored();
            await util.PopulateAllAsync();
            return util.BuildAnalyzeErrors;
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
