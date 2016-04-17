using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dashboard.Jenkins;
using System.Diagnostics;
using System.IO;
using RestSharp.Authenticators;

namespace Dashboard.ApiFun
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            // PrintMacTimes();
            // var util = new MachineCountInvestigation(CreateClient());
            // util.Go();
            // GetMacQueueTimes();
            // Random();
            // FindRetest();
            // PrintRetestInfo();
            // InspectReason(5567);
            // ScanAllFailedJobs();
            // PrintJobNames();
            // PrintJobInfo();
            // PrintQueue();
            // PrintViews();
            // PrintPullRequestData();
            // PrintFailure();
            // PrintJobs();

            /*
            roslyn_stabil_lin_dbg_unit32
            */
            Random();
        }

        private static JenkinsClient CreateClient(bool auth = true)
        {
            try
            {
                var text = File.ReadAllLines(@"c:\users\jaredpar\jenkins.txt")[0].Trim();
                if (string.IsNullOrEmpty(text) || !auth)
                {
                    return new JenkinsClient(SharedConstants.DotnetJenkinsUri);
                }

                var values = text.Split(':');
                return new JenkinsClient(SharedConstants.DotnetJenkinsUri, values[0], values[1]);
            }
            catch
            {
                return new JenkinsClient(SharedConstants.DotnetJenkinsUri);
            }
        }

        private static void Random()
        {
            var client = CreateClient();
            var jobId = JobId.ParseName("dotnet_buildtools");
            var buildIds = client.GetBuildIds(jobId);
        }


        private static void PrintJobs()
        {
            var client = CreateClient(auth: false);
            PrintJobs(client, JobId.Root, "");
        }

        private static void PrintJobs(JenkinsClient client, JobId parent, string indent)
        {
            foreach (var id in client.GetJobIds(parent))
            {
                Console.WriteLine($"{indent}{id.Name}");
                var info = client.GetJobInfo(id);
                if (info.Kind == JobKind.Folder)
                {
                    PrintJobs(client, id, indent + "  ");
                }
            }
        }

        private static void PrintFailure()
        {
            var client = CreateClient(auth: false);
            var info = client.GetBuildFailureInfo(new BuildId(id: 6066, jobId: JobId.ParseName("roslyn_prtest_win_dbg_unit64")));
            Console.WriteLine(info.Category);
        }

        private static void PrintViews()
        {
            foreach (var viewInfo in CreateClient().GetViews())
            {
                Console.WriteLine($"{viewInfo.Name} {viewInfo.Url}");
            }
        }

        private static void PrintQueue()
        {
            var client = CreateClient();
            foreach (var cur in client.GetQueuedItemInfoList())
            {
                Console.WriteLine($"{cur.JobId} {cur.Id} {cur.PullRequestInfo?.PullUrl ?? ""}");
            }
        }

        private static void PrintPullRequestData()
        {
            var client = CreateClient();
            var list = new List<Tuple<DateTime, BuildId>>();
            foreach (var jobId in client.GetJobIdsInView("Roslyn").Where(x => x.Name.Contains("prtest")).Where(x => !x.Name.Contains("internal")))
            {
                Console.WriteLine($"Job: {jobId}");
                foreach (var buildId in client.GetBuildIds(jobId))
                {
                    Console.WriteLine($"\tBuild: {buildId.Id}");
                    var date = client.GetBuildInfo(buildId).Date;
                    list.Add(Tuple.Create(date, buildId));
                }
            }

            foreach (var date in list.GroupBy(x => x.Item1.Date).OrderBy(x => x.Key))
            {
                if (date.Key.DayOfWeek == DayOfWeek.Saturday || date.Key.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                Console.WriteLine($"Date: {date.Key} Count: {date.Count()}");
            }
        }

        /// <summary>
        /// Print out the Mac times comparing the Mac Minis to the Powerbooks
        /// </summary>
        private static void PrintMacTimes()
        {
            var client = CreateClient();
            var miniTimes = new List<TimeSpan>();
            var proTimes = new List<TimeSpan>();

            foreach (var buildId in client.GetBuildIds(new JobId("roslyn_prtest_mac_dbg_unit32", JobId.Root)))
            {
                var result = client.GetBuildResult(buildId);
                if (result.State != BuildState.Succeeded)
                {
                    continue;
                }

                var time = result.BuildInfo.Duration;
                var json = client.GetJson(JenkinsUtil.GetBuildPath(buildId), tree: "builtOn");
                var name = json.Value<string>("builtOn");
                if (name.Contains("macpro"))
                {
                    proTimes.Add(time);
                }
                else
                {
                    miniTimes.Add(time);
                }
            }

            Console.WriteLine($"Pro Average: {TimeSpan.FromMilliseconds(proTimes.Average(x => x.TotalMilliseconds))}");
            Console.WriteLine($"Mini Average: {TimeSpan.FromMilliseconds(miniTimes.Average(x => x.TotalMilliseconds))}");
        }

        private static void GetMacQueueTimes()
        {
            var list = new List<int>();
            var client = CreateClient();
            foreach (var buildId in client.GetBuildIds(new JobId("roslyn_prtest_mac_dbg_unit32", JobId.Root)))
            {
                Console.WriteLine($"Processing {buildId.Id}");

                var state = client.GetBuildInfo(buildId).State;
                if (state == BuildState.Running)
                {
                    continue;
                }

                var time = client.GetTimeInQueue(buildId);
                if (time.HasValue)
                {
                    list.Add((int)time.Value.TotalMilliseconds);
                }
                else
                {
                    Console.WriteLine($"Could not get duration for {buildId.Id}");
                }
            }

            Action<string, TimeSpan> print = (msg, time) => Console.WriteLine($"{msg}: {time.ToString(@"hh\:mm\:ss")}");

            Console.WriteLine($"Total Jobs: {list.Count}");
            print("Average", TimeSpan.FromMilliseconds(list.Select(x => (double)x).Average()));
            print("Median", TimeSpan.FromMilliseconds(list.OrderBy(x => x).Skip(list.Count / 2).First()));
            print("Max", TimeSpan.FromMilliseconds(list.Max()));
            print("Min", TimeSpan.FromMilliseconds(list.Min()));
            Console.WriteLine($"Jobs < 30 min in queue: {list.Select(x => TimeSpan.FromMilliseconds(x)).Where(x => x < TimeSpan.FromMinutes(30)).Count()}");
        }

        private static void PrintJobInfo()
        {
            var client = CreateClient();
            foreach (var jobId in client.GetJobIds())
            {
                Console.WriteLine($"{jobId.Name}");
                foreach (var id in client.GetBuildIds(jobId))
                {
                    try
                    {
                        var info = client.GetBuildInfo(id);
                        Console.WriteLine($"\t{id} {info.State}");
                    }
                    catch
                    {
                        Console.WriteLine($"\t{id} can't read data");
                    }
                }
            }
        }

        private static void PrintRetestInfo()
        {
            /*
            var connectionString = File.ReadAllText(@"c:\users\jaredpar\connection.txt");
            var dataClient = new DataClient(connectionString);
            foreach (var info in dataClient.GetRetestInfo())
            {
                Console.WriteLine(info.JobId);
            }
            */
        }

        private static void ScanAllFailedJobs()
        {
            /*
            var client = new JenkinsClient();
            foreach (var buildId in client.GetJobIds(JobKind.WindowsDebug32))
            {
                Console.Write($"{buildId} ");
                try
                {
                    var jobResult = client.GetJobResult(buildId);
                    Console.WriteLine(jobResult.State);

                    if (jobResult.Failed)
                    {
                        if (jobResult.FailureInfo.Reason != JobFailureReason.Unknown)
                        {
                            Console.WriteLine(jobResult.FailureInfo.Reason);
                            continue;
                        }

                        Console.WriteLine("Ooops");
                    }
                }
                catch
                {
                    Console.WriteLine("more json to figure out");
                }
            }
            */
        }

        private static void InspectReason(int id)
        {
            /*
            var client = new JenkinsClient();
            var jobResult = client.GetJobResult(new JobId(id, JobKind.WindowsDebug32));
            Console.WriteLine(jobResult.FailureInfo.Reason);
            */
        }

        /*
        private static void PrintFailedJobs()
        {
            var client = new RoslynClient().Client;
            var names = client.GetJobNamesInView("roslyn");
            var jobIdList = client.GetBuildIds(names.ToArray());

            foreach (var cur in jobIdList)
            {
                var jobResult = client.GetBuildResult(cur);
                if (!jobResult.Failed)
                {
                    continue;
                }

                if (jobResult.FailureInfo.Messages.Contains("CS8032"))
                {
                    Console.WriteLine($"{cur.Id} {jobResult.FailureInfo.Reason}");
                }
            }
        }
        */

        private static void FindRetest()
        {
            /*
            var client = new JenkinsClient();
            var jobIdList = client.GetJobIds(JobKind.WindowsDebug32);
            var jobInfoList = new List<JobInfo>();

            foreach (var current in jobIdList)
            {
                try
                {
                    var jobInfo = client.GetJobInfo(current);
                    jobInfoList.Add(jobInfo);
                }
                catch
                {

                }
            }

            var data = jobInfoList.GroupBy(x => new BuildKey(x.PullRequestInfo.Id, x.PullRequestInfo.Sha1));
            foreach (var cur in data)
            {
                var all = cur.ToList();
                if (all.Count == 1)
                {
                    continue;
                }

                Console.WriteLine($"Pull Request {cur.Key.PullId} SHA {cur.Key.Sha1}");

                foreach (var job in cur)
                {
                    Console.WriteLine($"\t{job.Id}");
                }
            }
            */
        }
    }

    internal enum OS
    {
        Windows,
        Mac,
        Linux,
        FreeBSD,
        Unknown
    }

    internal static class Util
    {
        internal static OS ClassifyOperatingSystem(string os)
        {
            if (string.IsNullOrEmpty(os))
            {
                return OS.Unknown;
            }

            if (os.Contains("Linux"))
            {
                return OS.Linux;
            }

            if (os.Contains("Mac"))
            {
                return OS.Mac;
            }

            if (os.Contains("Windows"))
            {
                return OS.Windows;
            }

            if (os.Contains("FreeBSD"))
            {
                return  OS.FreeBSD;
            }

            return OS.Unknown;
        }
    }

    internal sealed class MachineCountInvestigation
    {
        private readonly JenkinsClient _client;
        private readonly Dictionary<string, OS> _computerNameMap = new Dictionary<string, OS>();

        internal MachineCountInvestigation(JenkinsClient client)
        {
            _client = client;
            BuildComputerNameMap();
        }

        internal void Go()
        {
            var yesterday = (DateTime.Today.Subtract(TimeSpan.FromDays(1))).Date;
            var list = new List<int>();
            for (int i = 0; i < 24; i++)
            {
                list.Add(0);
            }

            foreach (var buildId in GetBuildIds(os => os == OS.Mac))
            {
                var result = _client.GetBuildResult(buildId);
                if (result.BuildInfo.Date.Date != yesterday)
                {
                    continue;
                }

                var hour = result.BuildInfo.Date.Hour;
                list[hour]++;
            }

            for (int i = 0; i < list.Count; i++)
            {
                Console.WriteLine($"Hour {i} -> {list[i]}");
            }
        }

        internal void GoOld()
        {
            var yesterday = (DateTime.Today.Subtract(TimeSpan.FromDays(1))).Date;
            var linuxList = new List<TimeSpan>();
            var linuxQueueList = new List<TimeSpan>();
            var macList = new List<TimeSpan>();
            var macQueueList = new List<TimeSpan>();

            foreach (var buildId in GetBuildIds(os => os == OS.Mac || os == OS.Linux))
            {
                var result = _client.GetBuildResult(buildId);
                if (result.State == BuildState.Running)
                {
                    continue;
                }

                var queueTime = _client.GetTimeInQueue(buildId);
                if (!queueTime.HasValue)
                {
                    continue;
                }

                var os = GetOsForBuild(buildId);
                if (os == OS.Mac)
                {
                    macList.Add(result.BuildInfo.Duration);
                    macQueueList.Add(queueTime.Value);
                }
                if (os == OS.Linux)
                {
                    linuxList.Add(result.BuildInfo.Duration);
                    linuxQueueList.Add(queueTime.Value);
                }
            }

            Console.WriteLine($"Linux {linuxList.Count} builds");
            Console.WriteLine($"Linux {TimeSpan.FromMilliseconds(linuxList.Average(x => x.TotalMilliseconds))} average duration");
            Console.WriteLine($"Linux {TimeSpan.FromMilliseconds(linuxQueueList.Average(x => x.TotalMilliseconds))} average queue");
            Console.WriteLine($"Mac {macList.Count} builds");
            Console.WriteLine($"Mac {TimeSpan.FromMilliseconds(macList.Average(x => x.TotalMilliseconds))} average duration");
            Console.WriteLine($"Mac {TimeSpan.FromMilliseconds(macQueueList.Average(x => x.TotalMilliseconds))} average queue");
        }

        internal void GoQueueStats()
        {
            var linuxTimes = new List<TimeSpan>();
            var macTimes = new List<TimeSpan>();

            foreach (var buildId in GetBuildIds(os => os == OS.Mac || os == OS.Linux))
            {
                Console.WriteLine($"Processing {buildId.JobName} {buildId.Id}");

                var state = _client.GetBuildInfo(buildId).State;
                if (state == BuildState.Running)
                {
                    continue;
                }

                var queueTime = _client.GetTimeInQueue(buildId);
                if (!queueTime.HasValue)
                {
                    continue;
                }

                var os = GetOsForBuild(buildId);
                if (os == OS.Linux)
                {
                    linuxTimes.Add(queueTime.Value);
                }
                else if (os == OS.Mac)
                {
                    macTimes.Add(queueTime.Value);
                }
            }

            Console.WriteLine($"Linux average queue time {TimeSpan.FromMilliseconds(linuxTimes.Average(x => x.TotalMilliseconds))}");
            Console.WriteLine($"Linux machine count {_computerNameMap.Count(p => p.Value == OS.Linux)}");
            Console.WriteLine($"Mac average queue time {TimeSpan.FromMilliseconds(macTimes.Average(x => x.TotalMilliseconds))}");
            Console.WriteLine($"Mac machine count {_computerNameMap.Count(p => p.Value == OS.Mac)}");
        }

        private List<BuildId> GetBuildIds(Func<OS, bool> predicate)
        {
            var all = new List<BuildId>();
            foreach (var jobId in _client.GetJobIds())
            {
                try
                {
                    var list = _client.GetBuildIds(jobId);
                    if (list.Count > 0)
                    {
                        var os = GetOsForBuild(list[0]);
                        if (predicate(os))
                        {
                            all.AddRange(list);
                        }
                    }
                }
                catch
                {

                }
            }

            return all;
        }

        private OS GetOsForBuild(BuildId buildId)
        {
            var json = _client.GetJson(JenkinsUtil.GetBuildPath(buildId), tree: "builtOn[*]");
            var computer = json.Value<string>("builtOn");
            OS os;
            if (!string.IsNullOrEmpty(computer) && _computerNameMap.TryGetValue(computer, out os))
            {
                return os;
            }

            return OS.Unknown;
        }

        private void BuildComputerNameMap()
        {
            foreach (var cur in _client.GetComputerInfo())
            {
                _computerNameMap[cur.Name] = Util.ClassifyOperatingSystem(cur.OperatingSystem);
            }
        }

        private List<string> GetMacJobNames()
        {
            return _client
                .GetJobIds()
                .Select(x => x.Name)
                .Where(x => x.Contains("osx") || x.Contains("_mac_"))
                .ToList();
        }

        private List<string> GetLinuxJobNames()
        {
            return _client
                .GetJobIds()
                .Select(x => x.Name)
                .Where(x => x.Contains("ubuntu") || x.Contains("_lin_"))
                .Where(x => !x.Contains("_tst_"))
                .ToList();
        }
    }
}


