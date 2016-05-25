using Dashboard;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.ApiFun
{
    internal static class JenkinsDataUtil
    {
        internal struct BuildData
        {
            internal BuildId BuildId { get; set; }
            internal string ResultName { get; set; }
            internal string ResultCategory { get; set; }

            internal string ToLine()
            {
                return $"{BuildId.JobId.Name},{BuildId.Number},{ResultName},{ResultCategory}";
            }

            internal static BuildData ParseLine(string line)
            {
                var parts = line.Split(',');
                Debug.Assert(parts.Length == 4);
                var jobId = JobId.ParseName(parts[0]);
                var number = int.Parse(parts[1]);
                return new BuildData()
                {
                    BuildId = new BuildId(number, jobId),
                    ResultName = parts[2],
                    ResultCategory = parts[3]
                };
            }
        }

        internal const string Root = @"c:\users\jaredpar\temp\data\";
        internal static readonly string JobListFile = Path.Combine(Root, "jobs.txt");

        internal static void Go()
        {
            CollectData();
            // ProcessUnknown().Wait();
        }

        private static async Task ProcessUnknown()
        {
            foreach (var jobId in GetJobList().Where(x => !x.Item1).Select(x => x.Item2))
            {
                var list = ReadBuildData(jobId);
                var anyChanged = false;
                for (int i = 0; i < list.Count; i++)
                {
                    var buildData = list[i];
                    Console.WriteLine($"{jobId.Name} - {buildData.BuildId.Number}");
                    if (buildData.ResultCategory == "Unknown" && await IsJavaFailure(buildData.BuildId))
                    {
                        buildData.ResultName = "Java Illegal State";
                        buildData.ResultCategory = "Infrastructure";
                        anyChanged = true;
                        list[i] = buildData;
                    }
                }

                if (anyChanged)
                {
                    WriteBuildDataList(jobId, list);
                }
            }
        }

        private static async Task<bool> IsJavaFailure(BuildId buildId)
        {
            var client = CreateClient();
            try
            {
                var consoleText = await client.GetConsoleTextAsync(buildId);
                if (consoleText.Contains("java.lang.IllegalStateException: Invalid"))
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static void Process()
        {
            ProcessJobList().Wait();
        }

        private static void CollectData()
        {
            var all = new List<string>();
            foreach (var file in Directory.EnumerateFiles(Root, "builds.csv", SearchOption.AllDirectories))
            {
                foreach (var buildData in ReadBuildData(file))
                {
                    var path = JenkinsUtil.ConvertJobIdToPath(buildData.BuildId.JobId);
                    var isPr = JobUtil.IsPullRequestJobName(buildData.BuildId.JobId);
                    var newLine = $"{path},{buildData.BuildId.Number},{isPr},{buildData.ResultName},{buildData.ResultCategory}";
                    all.Add(newLine);
                }
            }

            File.WriteAllLines(
                Path.Combine(Root, "all.csv"),
                all.ToArray());
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

        private static List<Tuple<bool, JobId>> GetJobList()
        {
            var list = new List<Tuple<bool, JobId>>();
            foreach (var line in File.ReadAllLines(JobListFile))
            {
                var parts = line.Split(new[] { ':' }, count: 2, options: StringSplitOptions.None);
                var isFolder = bool.Parse(parts[0]);
                var jobId = JobId.ParseName(parts[1]);
                list.Add(Tuple.Create(isFolder, jobId));
            }

            return list;
        }

        private static async Task ProcessJobList()
        {
            var jobList = GetJobList()
                .Where(x => !x.Item1)
                .Select(x => x.Item2);

            await ProcessJobListCore(jobList);
        }

        private static string GetJobFolder(JobId jobId)
        {
            var jobDataPath = Path.Combine(Root, "jobs");
            var folder = Path.Combine(jobDataPath, jobId.Name.Replace('/', '_'));
            return folder;
        }

        private static List<BuildData> ReadBuildData(JobId jobId)
        {
            var file = Path.Combine(GetJobFolder(jobId), "builds.csv");
            return ReadBuildData(file);
        }

        private static List<BuildData> ReadBuildData(string buildFile)
        { 
            var list = new List<BuildData>();
            foreach (var line in File.ReadAllLines(buildFile))
            {
                list.Add(BuildData.ParseLine(line));
            }

            return list;
        }

        private static async Task ProcessJobListCore(IEnumerable<JobId> jobIdList)
        {
            var toProcess = new Queue<JobId>(jobIdList);
            var maxRunners = Environment.ProcessorCount * 4;
            var running = new List<Task>();
            var errorCount = 0;
            while (toProcess.Count > 0)
            {
                while (running.Count < maxRunners && toProcess.Count > 0)
                {
                    var jobId = toProcess.Dequeue();
                    var folder = GetJobFolder(jobId);
                    running.Add(ProcessJobIdAsync(folder, jobId));
                }

                try
                {
                    await Task.WhenAny(running).ConfigureAwait(true);
                }
                catch
                {
                    errorCount++;
                }

                errorCount += running.Count(x => x.IsFaulted);
                running.RemoveAll(x => x.IsCompleted);
            }

            try
            {
                await Task.WhenAll(running).ConfigureAwait(true);
            }
            catch
            {
                errorCount++;
            }

            Console.WriteLine($"Finished with {errorCount} errors");
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

            var list = await ProcessBuildsAsync(jobId);
            WriteBuildDataList(jobId, list);
            File.WriteAllText(donePath, "done");
        }

        private static void WriteBuildDataList(JobId jobId, List<BuildData> list)
        { 
            var jobDataFolder = GetJobFolder(jobId);
            var lines = list
                .Select(x => x.ToLine())
                .ToArray();

            CreateDirectory(jobDataFolder);
            File.WriteAllLines(
                Path.Combine(jobDataFolder, "builds.csv"),
                lines);
        }

        private static async Task<List<BuildData>> ProcessBuildsAsync(JobId jobId)
        {
            var client = CreateClient();
            var list = new List<BuildData>();
            var builds = await client.GetBuildIdsAsync(jobId);
            foreach (var buildId in builds)
            {
                var buildData = await GetBuildDataAsync(buildId);
                if (buildData.HasValue)
                {
                    list.Add(buildData.Value);
                }
            }

            return list;
        }

        private static async Task<BuildData?> GetBuildDataAsync(BuildId buildId)
        {
            var client = CreateClient();
            var buildInfo = await client.GetBuildInfoAsync(buildId);
            string name;
            string category;
            switch (buildInfo.State)
            {
                case BuildState.Succeeded:
                    name = "Succeeded";
                    category = "Succeeded";
                    break; ;
                case BuildState.Aborted:
                    name = "Aborted";
                    category = "Aborted";
                    break; ;
                case BuildState.Failed:
                    {
                        try
                        {
                            var buildResult = await client.GetBuildResultAsync(buildInfo);
                            var cause = GetBestCause(buildResult.FailureInfo);
                            name = string.IsNullOrEmpty(cause.Name) ? "Unknown Name" : cause.Name;
                            category = string.IsNullOrEmpty(cause.Category) ? "Unknown Category" : cause.Category;
                        }
                        catch
                        {
                            name = "Rest API Error";
                            category = "Unknown";
                        }
                    }
                    break;
                case BuildState.Running:
                    // Don't collect data here
                    return null;
                default:
                    Debug.Assert(false);
                    return null;
            }

            return new BuildData()
            {
                BuildId = buildId,
                ResultName = name,
                ResultCategory = category,
            };
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
