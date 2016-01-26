using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public static class JenkinsUtil
    {
        private static Uri GetUri(Uri baseUrl, string path)
        {
            var builder = new UriBuilder(baseUrl);
            builder.Path = path;
            return builder.Uri;
        }

        public static string GetJobPath(JobId id)
        {
            return $"job/{id.Name}/{id.Id}/";
        }

        public static Uri GetJobUri(Uri baseUrl, JobId id)
        {
            return GetUri(baseUrl, GetJobPath(id));
        }

        public static string GetConsoleTextPath(JobId id)
        {
            return $"{GetJobPath(id)}consoleText";
        }

        public static Uri GetConsoleTextUri(Uri baseUrl, JobId id)
        {
            return GetUri(baseUrl, GetConsoleTextPath(id));
        }
    }
}
