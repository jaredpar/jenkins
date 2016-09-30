using System;
using System.Linq;
using System.Text;

namespace Dashboard.Jenkins
{
    public static class JenkinsUtil
    {
        public static Uri GetUri(Uri baseUrl, string path)
        {
            var builder = new UriBuilder(baseUrl);
            builder.Path = path;
            return builder.Uri;
        }

        public static Uri GetUri(Uri baseUrl, BuildId buildId)
        {
            var path = GetBuildPath(buildId);
            return GetUri(baseUrl, path);
        }

        public static Uri GetUri(Uri baseUrl, JobId jobId)
        {
            var path = GetJobIdPath(jobId);
            return GetUri(baseUrl, path);
        }

        public static string GetJobIdPath(JobId id)
        {
            if (id.IsRoot)
            {
                return "";
            }

            if (id.Parent.IsRoot)
            {
                return $"job/{id.ShortName}";
            }

            return $"{GetJobIdPath(id.Parent)}/job/{id.ShortName}";
        }

        public static string GetBuildPath(BuildId id)
        {
            return $"{GetJobIdPath(id.JobId)}/{id.Number}/";
        }

        public static Uri GetBuildStatusIconUri(Uri jenkinsUri, BuildId id)
        {
            var builder = new UriBuilder(jenkinsUri);
            builder.Path = "buildStatus/icon";
            builder.Query = $"job={id.JobId.Name}&build={id.Number}";
            return builder.Uri;
        }

        public static string GetQueuedItemPath(int number)
        {
            return $"queue/item/{number}";
        }

        public static string GetConsoleTextPath(BuildId id)
        {
            return $"{GetBuildPath(id)}consoleText";
        }

        public static string GetTestReportPath(BuildId id)
        {
            return $"{GetBuildPath(id)}testReport";
        }

        public static string GetJobDeletePath(JobId jobId)
        {
            return $"{GetJobIdPath(jobId)}/doDelete";
        }

        public static string GetJobEnablePath(JobId jobId)
        {
            return $"{GetJobIdPath(jobId)}/enable";
        }

        public static string GetJobDisablePath(JobId jobId)
        {
            return $"{GetJobIdPath(jobId)}/disable";
        }

        /// <summary>
        /// Jenkins expresses all dates in a value typically named timestamp.  This seconds since the
        /// epoch.  This function will convert the Jenkins representation to a <see cref="DateTime"/>
        /// value.
        /// <returns></returns>
        public static DateTimeOffset ConvertTimestampToDateTimeOffset(long timestamp)
        {
            var epoch = new DateTimeOffset(year: 1970, month: 1, day: 1, hour: 0, minute: 0, second: 0, offset: TimeSpan.Zero);
            return epoch.AddMilliseconds(timestamp);
        }

        public static JobId ConvertPathToJobId(string path)
        {
            JobId jobId;
            if (!TryConvertPathToJobId(path, out jobId))
            {
                throw new Exception($"Not a valid job path: {path}");
            }

            return jobId;
        }

        public static bool TryConvertPathToJobId(string path, out JobId jobId)
        {
            var items = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length % 2 != 0)
            {
                jobId = null;
                return false;
            }

            var current = JobId.Root;
            for (var i = 0; i < items.Length; i+=2)
            {
                if (items[i] != "job")
                {
                    jobId = null;
                    return false;
                }

                current = new JobId(shortName: items[i + 1], parent: current);
            }

            jobId = current;
            return true;
        }

        public static BuildId ConvertPathToBuildId(string path)
        {
            BuildId buildId;
            if (!TryConvertPathToBuildId(path, out buildId))
            {
                throw new Exception($"Not a valid build id path: {path}");
            }

            return buildId;
        }

        public static bool TryConvertPathToBuildId(string path, out BuildId buildId)
        {
            buildId = default(BuildId);

            if (path.Contains('?'))
            {
                return false;
            }

            var parts = path.Split(new[] { '/' }, options: StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            int number;
            if (!int.TryParse(parts[parts.Length - 1], out number))
            {
                return false;
            }

            var jobPath = string.Join("/", parts.Take(parts.Length - 1));
            JobId jobId;
            if (!TryConvertPathToJobId(jobPath, out jobId))
            {
                return false;
            }

            buildId = new BuildId(number, jobId);
            return true;
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
