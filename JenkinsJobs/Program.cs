using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Roslyn.Jenkins;
using Roslyn.Azure;
using Roslyn;

namespace JenkinsJobs
{
    internal sealed class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        public static void Main()
        {
            /*
            var host = new JobHost();
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
            */
            LocalRun().Wait();
        }

        private static async Task LocalRun()
        {
            try
            {
                var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var tableClient = storageAccount.CreateCloudTableClient();
                var buildFailureTable = tableClient.GetTableReference(AzureConstants.TableNameBuildFailure);
                buildFailureTable.CreateIfNotExists();
                var buildProcessedTable = tableClient.GetTableReference(AzureConstants.TableNameBuildProcessed);
                buildProcessedTable.CreateIfNotExists();

                // TODO: Need a Jenkins token as well to be able to query our non-public jobs.
                var githubConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.GithubConnectionStringName);
                var roslynClient = string.IsNullOrEmpty(githubConnectionString)
                    ? new RoslynClient()
                    : new RoslynClient(connectionString: githubConnectionString);

                var util = new JobTableUtil(buildProcessedTable: buildProcessedTable, buildFailureTable: buildFailureTable, roslynClient: roslynClient, textWriter: Console.Out);
                await util.MoveUnknownToIgnored();
                await util.Populate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to run the local request: {ex}");
            }
        }
    }
}
