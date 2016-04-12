using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Roslyn;
using Roslyn.Azure;
using Roslyn.Jenkins;

namespace JenkinsJobs
{
    public class Functions
    {
        public static async Task PopulateBuildTables(TextWriter logger)
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

                var util = new JobTableUtil(buildProcessedTable: buildProcessedTable, buildFailureTable: buildFailureTable, roslynClient: roslynClient, textWriter: logger);
                await util.MoveUnknownToIgnored();
                await util.Populate();
            }
            catch (Exception ex)
            {
                logger.WriteLine($"Unable to run the local request: {ex}");
            }
        }
    }
}
