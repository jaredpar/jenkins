using Dashboard.Models;
using Roslyn.Jenkins;
using Roslyn.Sql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Dashboard.Helpers
{
    public class DashboardController : Controller
    {
        protected RoslynClient CreateRoslynClient()
        {
            // TODO: use authenticated client
            return new RoslynClient();
        }

        protected JenkinsClient CreateJenkinsClient()
        {
            return CreateRoslynClient().Client;
        }

    }
}