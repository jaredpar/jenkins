using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Roslyn.Jenkins;
using Roslyn.Sql;
using System.Diagnostics;
using System.IO;
using RestSharp.Authenticators;

namespace ApiFun
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            var util = new MachineCountInvestigation(CreateClient());
            util.Go();
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

            /*
            roslyn_stabil_lin_dbg_unit32
            */
        }

        private static RoslynClient CreateClient()
        {
            try
            {
                var text = File.ReadAllLines(@"c:\users\jaredpar\jenkins.txt")[0].Trim();
                if (string.IsNullOrEmpty(text))
                {
                    return new RoslynClient();
                }

                var values = text.Split(':');
                return new RoslynClient(values[0], values[1]);
            }
            catch
            {
                return new RoslynClient();
            }
        }

        private static void PrintViews()
        {
            foreach (var viewInfo in CreateClient().Client.GetViews())
            {
                Console.WriteLine($"{viewInfo.Name} {viewInfo.Url}");
            }
        }

        private static void PrintQueue()
        {
            var client = CreateClient().Client;
            foreach (var cur in client.GetQueuedItemInfo())
            {
                Console.WriteLine($"{cur.JobName} {cur.Id} {cur.PullRequestInfo?.PullUrl ?? ""}");
            }
        }

        private static void GetMacQueueTimes()
        {
            var list = new List<int>();
            var client = CreateClient();
            foreach (var buildId in client.Client.GetBuildIds("roslyn_prtest_mac_dbg_unit32"))
            {
                Console.WriteLine($"Processing {buildId.Id}");

                var state = client.Client.GetBuildState(buildId);
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

        private static void PrintJobNames()
        {
            var client = CreateClient();
            foreach (var name in client.GetJobNames().Concat(client.Client.GetJobNamesInView("roslyn-internal")))
            {
                Console.WriteLine(name);
            }
        }

        private static void PrintJobInfo()
        {
            var roslynClient = CreateClient();
            var client = roslynClient.Client;
            foreach (var name in roslynClient.GetJobNames())
            {
                Console.WriteLine($"{name}");
                foreach (var id in client.GetBuildIds(name))
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

        private static void Random()
        {
            var client = new RoslynClient().Client;
            var list = client.GetQueuedItemInfo();

            var query = list
                .Where(x => x.PullRequestInfo != null)
                .GroupBy(x => x.PullRequestInfo.PullUrl)
                .OrderByDescending(x => x.Count());

            foreach (var pr in query)
            {
                Console.WriteLine($"{pr.Key} {pr.First().PullRequestInfo.AuthorEmail} {pr.Count()}");
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

    internal sealed class MachineCountInvestigation
    {
        private enum OS
        {
            Windows,
            Mac,
            Linux,
            FreeBSD,
            Unknown
        }

        private readonly RoslynClient _roslynClient;
        private readonly JenkinsClient _client;
        private readonly Dictionary<string, OS> _computerNameMap = new Dictionary<string, OS>();

        internal MachineCountInvestigation(RoslynClient roslynClient)
        {
            _roslynClient = roslynClient;
            _client = roslynClient.Client;
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
                if (result.JobInfo.Date.Date != yesterday)
                {
                    continue;
                }

                var hour = result.JobInfo.Date.Hour;
                list[hour]++;
            }

            for (int i =0; i < list.Count;i ++)
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

                var queueTime = _roslynClient.GetTimeInQueue(buildId);
                if (!queueTime.HasValue)
                {
                    continue;
                }

                var os = GetOsForBuild(buildId);
                if (os == OS.Mac)
                {
                    macList.Add(result.JobInfo.Duration);
                    macQueueList.Add(queueTime.Value);
                }
                if (os == OS.Linux)
                {
                    linuxList.Add(result.JobInfo.Duration);
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
                Console.WriteLine($"Processing {buildId.Name} {buildId.Id}");

                var state = _client.GetBuildState(buildId);
                if (state == BuildState.Running)
                {
                    continue;
                }

                var queueTime = _roslynClient.GetTimeInQueue(buildId);
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
            foreach (var jobName in _client.GetJobNames())
            {
                // TODO: temp., delete 
                if (!jobName.Contains("roslyn"))
                {
                    continue; 
                }

                try
                {
                    var list = _client.GetBuildIds(jobName);
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
                if (string.IsNullOrEmpty(cur.OperatingSystem))
                {
                    _computerNameMap[cur.Name] = OS.Unknown;
                }
                else if (cur.OperatingSystem.Contains("Linux"))
                {
                    _computerNameMap[cur.Name] = OS.Linux;
                }
                else if (cur.OperatingSystem.Contains("Mac"))
                {
                    _computerNameMap[cur.Name] = OS.Mac;
                }
                else if (cur.OperatingSystem.Contains("Windows"))
                {
                    _computerNameMap[cur.Name] = OS.Windows;
                }
                else if (cur.OperatingSystem.Contains("FreeBSD"))
                {
                    _computerNameMap[cur.Name] = OS.FreeBSD;
                }
                else
                {
                    _computerNameMap[cur.Name] = OS.Unknown;
                }
            }
        }

        private List<string> GetMacJobNames()
        {
            return _client
                .GetJobNames()
                .Where(x => x.Contains("osx") || x.Contains("_mac_"))
                .ToList();
        }

        private List<string> GetLinuxJobNames()
        {
            return _client
                .GetJobNames()
                .Where(x => x.Contains("ubuntu") || x.Contains("_lin_"))
                .Where(x => !x.Contains("_tst_"))
                .ToList();
        }
    }
}


