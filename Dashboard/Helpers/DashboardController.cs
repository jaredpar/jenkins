using Dashboard.Models;
using Dashboard.Jenkins;
using Dashboard.Sql;
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
        protected JenkinsClient CreateJenkinsClient()
        {
            // TODO: authentication? 
            return new JenkinsClient(SharedConstants.DotnetJenkinsUri);
        }
    }
}