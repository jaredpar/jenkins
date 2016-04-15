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

        public static string GetJobIdPath(JobId id)
        {
            if (id.IsRoot)
            {
                return "";
            }

            if (id.Parent.IsRoot)
            {
                return $"job/{id.Name}";
            }

            return $"{GetJobIdPath(id.Parent)}/job/{id.Name}";
        }

        public static string GetJobPath(string jobName)
        {
            return $"job/{jobName}";
        }

        public static string GetBuildPath(BuildId id)
        {
            return $"job/{id.JobName}/{id.Id}/";
        }

        public static Uri GetBuildUri(Uri baseUrl, BuildId id)
        {
            return GetUri(baseUrl, GetBuildPath(id));
        }

        public static string GetConsoleTextPath(BuildId id)
        {
            return $"{GetBuildPath(id)}consoleText";
        }

        public static Uri GetConsoleTextUri(Uri baseUrl, BuildId id)
        {
            return GetUri(baseUrl, GetConsoleTextPath(id));
        }

        public static string GetTestReportPath(BuildId id)
        {
            return $"{GetBuildPath(id)}testReport";
        }

        public static Uri GetTestReportUri(Uri baseUrl, BuildId id)
        {
            return GetUri(baseUrl, GetTestReportPath(id));
        }
    }
}
