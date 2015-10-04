using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Dashboard.Startup))]
namespace Dashboard
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
