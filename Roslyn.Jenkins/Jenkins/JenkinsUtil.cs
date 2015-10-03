using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public static class JenkinsUtil
    {
        public static readonly Uri JenkinsHost = new Uri("http://dotnet-ci.cloudapp.net");

        private static Uri GetUri(string path)
        {
            var builder = new UriBuilder(JenkinsHost);
            builder.Path = path;
            return builder.Uri;
        }

        public static string GetJobPath(JobId id)
        {
            return $"job/{id.Name}/{id.Id}/";
        }

        public static Uri GetJobUri(JobId id)
        {
            return GetUri(GetJobPath(id));
        }

        public static string GetConsoleTextPath(JobId id)
        {
            return $"{GetJobPath(id)}consoleText";
        }

        public static Uri GetConsoleTextUri(JobId id)
        {
            return GetUri(GetConsoleTextPath(id));
        }
    }
}
