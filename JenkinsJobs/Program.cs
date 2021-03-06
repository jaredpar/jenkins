﻿using Microsoft.Azure.WebJobs;
using Dashboard.Azure;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;

namespace Dashboard.StorageBuilder
{
    internal sealed class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        public static void Main()
        {
            var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            var storage = CloudStorageAccount.Parse(connectionString);
            AzureUtil.EnsureAzureResources(storage);

            // Manually set the values vs. reading from connectionStrings.  Developing with connectionString
            // values is dangerous because you have to keep the password in the developer directory.  Can't use
            // relative source paths to find it above it.  So keep using appSettings here and just copy the 
            // values over.
            var config = new JobHostConfiguration();
            config.DashboardConnectionString = connectionString;
            config.StorageConnectionString = connectionString;
            config.UseTimers();

#if DEBUG
            config.Tracing.ConsoleLevel = System.Diagnostics.TraceLevel.Verbose;
#endif

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}
