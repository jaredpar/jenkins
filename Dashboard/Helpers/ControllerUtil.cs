using Dashboard.Azure;
using Dashboard.Jenkins;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;

namespace Dashboard.Helpers
{
    internal static class ControllerUtil
    {
        private static CounterStatsUtil _counterStatsUtil;

        internal static CloudStorageAccount CreateStorageAccount()
        {
            var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            return CloudStorageAccount.Parse(connectionString);
        }

        internal static JenkinsClient CreateJenkinsClient()
        {
            var connectionString = ConfigurationManager.AppSettings[SharedConstants.GithubConnectionStringName];
            return new JenkinsClient(SharedConstants.DotnetJenkinsUri, connectionString);
        }

        internal static CounterStatsUtil GetOrCreateCounterStatsUtil(CloudStorageAccount account)
        {
            var util = _counterStatsUtil;
            if (util == null)
            {
                _counterStatsUtil = new CounterStatsUtil(account.CreateCloudTableClient());
                util = _counterStatsUtil;
            }

            return util;
        }
    }
}