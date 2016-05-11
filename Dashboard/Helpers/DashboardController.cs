using Dashboard.Models;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Dashboard.Azure;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using System.Web.Http;

namespace Dashboard.Helpers
{
    public class DashboardController : Controller
    {
        public DashboardStorage Storage { get; }
        public CloudStorageAccount StorageAccount => Storage.StorageAccount;

        protected DashboardController()
        {
            var dashboardConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            Storage = new DashboardStorage(dashboardConnectionString);
        }

        protected JenkinsClient CreateJenkinsClient()
        {
            var connectionString = ConfigurationManager.AppSettings[SharedConstants.GithubConnectionStringName];
            return new JenkinsClient(SharedConstants.DotnetJenkinsUri, connectionString);
        }
    }

    public class DashboardApiController : ApiController
    {
        public DashboardStorage Storage { get; }
        public CloudStorageAccount StorageAccount => Storage.StorageAccount;

        protected DashboardApiController()
        {
            var dashboardConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            Storage = new DashboardStorage(dashboardConnectionString);
        }

        protected JenkinsClient CreateJenkinsClient()
        {
            // TODO: authentication? 
            return new JenkinsClient(SharedConstants.DotnetJenkinsUri);
        }
    }
}