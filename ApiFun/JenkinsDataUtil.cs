using Dashboard;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.ApiFun
{
    internal static class JenkinsDataUtil
    {
        internal static void Process()
        {
            var root = @"c:\users\jaredpar\temp\data\";
            var jobList = Path.Combine(root, "jobs.txt");
            ProcessJobList(jobFile: jobList, buildRootPath: root).Wait();
        }

        private static JenkinsClient CreateClient()
        {
            return new JenkinsClient(SharedConstants.DotnetJenkinsUri);
        }

        private static async Task WriteJobList(string jobFile)
        {
            var toVisit = new Queue<JobId>();
            toVisit.Enqueue(JobId.Root);
            var seen = new HashSet<JobId>();
            var list = new List<Tuple<bool, JobId>>();
            var client = CreateClient();

            while (toVisit.Count > 0)
            {
                var current = toVisit.Dequeue();
                if (!seen.Add(current))
                {
                    continue;
                }

                Console.WriteLine(current.Name);
                var children = await client.GetJobIdsAsync(current);
                var isFolder = children.Count > 0;
                if (children.Count > 0)
                {
                    children.ForEach(x => toVisit.Enqueue(x));
                }

                list.Add(Tuple.Create(isFolder, current));
            }

            File.WriteAllLines(
                jobFile,
                list.Select(x => $"{x.Item1}:{x.Item2.Name}").ToArray());
        }

        private static async Task ProcessJobList(string jobFile, string buildRootPath)
        {
            var jobDataPath = Path.Combine(buildRootPath, "jobs");
            var toProcess = new Queue<JobId>();
            foreach (var line in File.ReadAllLines(jobFile))
            {
                var parts = line.Split(new[] { ':' }, count: 2, options: StringSplitOptions.None);
                var isFolder = bool.Parse(parts[0]);
                if (isFolder)
                {
                    continue;
                }

                var jobId = JobId.ParseName(parts[1]);
                toProcess.Enqueue(jobId);
            }

            var maxRunners = Environment.ProcessorCount * 2;
            var running = new List<Task>();
            while (toProcess.Count > 0)
            {
                while (running.Count < maxRunners)
                {
                    var jobId = toProcess.Dequeue();
                    var folder = Path.Combine(jobDataPath, jobId.Name.Replace('/', '_'));
                    Console.WriteLine(jobId.Name);
                    running.Add(ProcessJobIdAsync(folder, jobId));
                }

                await Task.WhenAny(running).ConfigureAwait(true);
                running.RemoveAll(x => x.IsCompleted);
            }

            await Task.WhenAll(running).ConfigureAwait(true);
        }

        /// <summary>
        /// Write out the file with the format
        ///
        ///     job_name, build number, result name, result category
        /// </summary>
        private static async Task ProcessJobIdAsync(string jobDataFolder, JobId jobId)
        {
            var donePath = Path.Combine(jobDataFolder, "done");
            if (File.Exists(donePath))
            {
                return;
            }

            var client = CreateClient();
            var list = new List<string>();
            var builds = await client.GetBuildIdsAsync(jobId);
            foreach (var buildId in builds)
            {
                var buildInfo = await client.GetBuildInfoAsync(buildId);
                if (buildInfo.State == BuildState.Succeeded)
                {
                    list.Add($"{jobId.Name},{buildId.Number},Succeeded,Succeeded");
                    continue;
                }

                var buildResult = await client.GetBuildResultAsync(buildInfo);
                var cause = GetBestCause(buildResult.FailureInfo);
                var name = string.IsNullOrEmpty(cause.Name) ? "Unknown Name" : cause.Name;
                var category = string.IsNullOrEmpty(cause.Category) ? "Unknown Category" : cause.Category;
                list.Add($"{jobId.Name},{buildId.Number},{name},{category}");
            }

            CreateDirectory(jobDataFolder);
            File.WriteAllLines(
                Path.Combine(jobDataFolder, "builds.csv"),
                list.ToArray());
            File.WriteAllText(donePath, "done");
        }

        private static BuildFailureCause GetBestCause(BuildFailureInfo info)
        {
            if (info == null)
            {
                return BuildFailureCause.Unknown;
            }

            var cause = BuildFailureCause.Unknown;
            foreach (var current in info.CauseList)
            {
                if (cause.Category == BuildFailureCause.CategoryMergeConflict || cause.Category == BuildFailureCause.CategoryUnknown)
                {
                    cause = current;
                }
            }

            return cause;
        }

        internal static bool CreateDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
