using Dashboard.Azure;
using Dashboard.Helpers;
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

            AzureUtil.EnsureAzureResources(ControllerUtil.CreateStorageAccount());
        }
    }
}
