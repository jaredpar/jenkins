using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;

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
            LocalRun();
        }

        private static void LocalRun()
        {
            try
            {
                var connectionString = CloudConfigurationManager.GetSetting("jaredpar-storage-connectionstring");
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var tableClient = storageAccount.CreateCloudTableClient();
                var table = tableClient.GetTableReference("JobFailure");
                table.CreateIfNotExists();

                var util = new JobTableUtil(table);
                util.Populate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to run the local request: {ex}");
            }
        }
    }
}
