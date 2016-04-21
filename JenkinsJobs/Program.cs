using System;
using Microsoft.Azure.WebJobs;
using Dashboard.Azure;
using Microsoft.WindowsAzure;

namespace Dashboard.StorageBuilder
{
    internal sealed class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        public static void Main()
        {
            var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            var storage = new DashboardStorage(connectionString);
            storage.EnsureAzureResources();

            Functions.PopulateBuildTables(Console.Out).Wait();
            /*
            var host = new JobHost();
            host.RunAndBlock();
            */
        }
    }
}
