using Dashboard.Azure;
using Dashboard.Jenkins;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;

namespace Dashboard.Helpers
{
    internal static class ControllerUtil
    {
        internal static CloudStorageAccount CreateStorageAccount()
        {
            var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            return CloudStorageAccount.Parse(connectionString);
        }

        internal static DashboardStorage CreateDashboardStorage()
        {
            var storage = CreateStorageAccount();
            return new DashboardStorage(storage);
        }

        internal static JenkinsClient CreateJenkinsClient()
        {
            var connectionString = ConfigurationManager.AppSettings[SharedConstants.GithubConnectionStringName];
            return new JenkinsClient(SharedConstants.DotnetJenkinsUri, connectionString);
        }
    }
}