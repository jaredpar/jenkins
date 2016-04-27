using Dashboard.Azure;
using Microsoft.Owin;
using Microsoft.WindowsAzure;
using Owin;

[assembly: OwinStartupAttribute(typeof(Dashboard.Startup))]
namespace Dashboard
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            var storage = new DashboardStorage(connectionString);
            AzureUtil.EnsureAzureResources(storage.StorageAccount);
        }
    }
}
