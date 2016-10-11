using System;

namespace Dashboard
{
    public static class SharedConstants
    {
        // TODO: Delete this.  Should no longer be generally available as there are lots of Jenkins servers
        // to consider now.
        public const string DotnetJenkinsUriString = "https://dotnet-ci.cloudapp.net";
        public static readonly Uri DotnetJenkinsUri = new Uri(DotnetJenkinsUriString);
        public static readonly string DotnetJenkinsHostName = DotnetJenkinsUri.Host;

        public const string DashboardUriString = "https://jdash.azurewebsites.net";
        public static readonly Uri DashboardUri = new Uri(DashboardUriString);

#if DEBUG
        public const string DashboardDebugUriString = "http://localhost:9859";
#else
        public const string DashboardDebugUriString = DashboardUriString;
#endif
        public static readonly Uri DashboardDebugUri = new Uri(DashboardDebugUriString);

        public const string SendGridApiKeySettingName = "sendgrid-api-key";
        public const string StorageConnectionStringName = "azure-storage-connection-string";
        public const string GithubConnectionStringName = "github-connection-string";
    }
}
