using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard
{
    public static class SharedConstants
    {
        public const string DotnetJenkinsUriString = "https://dotnet-ci.cloudapp.net";
        public static readonly Uri DotnetJenkinsUri = new Uri(DotnetJenkinsUriString);

        public const string DashboardUriString = "http://jdash.azurewebsites.net";
        public static readonly Uri DashboardUri = new Uri(DashboardUriString);

        public const string SendGridApiKeySettingName = "sendgrid-api-key";
        public const string StorageConnectionStringName = "jaredpar-storage-connection-string";
        public const string GithubConnectionStringName = "github-connection-string";
        public const string SqlConnectionStringName = "jenkins-connection-string";
    }
}
