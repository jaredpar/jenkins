using System;
using Microsoft.Azure.WebJobs;

namespace JenkinsJobs
{
    internal sealed class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        public static void Main()
        {
            Functions.PopulateBuildTables(Console.Out).Wait();
            /*
            var host = new JobHost();
            host.RunAndBlock();
            */
        }
    }
}
