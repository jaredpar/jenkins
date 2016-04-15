using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public static class JenkinsUtil
    {
        public static Uri GetUri(Uri baseUrl, string path)
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

        public static string GetConsoleTextPath(BuildId id)
        {
            return $"{GetBuildPath(id)}consoleText";
        }

        public static string GetTestReportPath(BuildId id)
        {
            return $"{GetBuildPath(id)}testReport";
        }

        /// <summary>
        /// Jenkins expresses all dates in a value typically named timestamp.  This seconds since the
        /// epoch.  This function will convert the Jenkins representation to a <see cref="DateTime"/>
        /// value.
        /// <returns></returns>
        public static DateTime ConvertTimestampToDateTime(long timestamp)
        {
            var epoch = new DateTime(year: 1970, month: 1, day: 1);
            return epoch.AddMilliseconds(timestamp).ToUniversalTime();
        }

        public static JobId ConvertPathToJobId(string path)
        {
            var items = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length % 2 != 0)
            {
                throw new Exception();
            }

            var current = JobId.Root;
            for (var i = 0; i < items.Length; i+=2)
            {
                if (items[i] != "job")
                {
                    throw new Exception();
                }

                current = new JobId(shortName: items[i + 1], parent: current);
            }

            return current;
        }

        public static string ConvertJobIdToPath(JobId jobId)
        {
            var builder = new StringBuilder();
            ConvertJobIdToPathCore(builder, jobId);
            return builder.ToString();
        }

        private static void ConvertJobIdToPathCore(StringBuilder builder, JobId jobId)
        {
            if (jobId.IsRoot)
            {
                return;
            }

            ConvertJobIdToPathCore(builder, jobId.Parent);

            if (builder.Length != 0)
            {
                builder.Append('/');
            }

            builder.Append($"job/{jobId.ShortName}");
        }
    }
}
