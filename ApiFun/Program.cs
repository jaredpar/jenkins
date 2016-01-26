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
            // GetMacQueueTimes();
            // Random();
            // FindRetest();
            // PrintRetestInfo();
            // InspectReason(5567);
            // ScanAllFailedJobs();
            // PrintJobNames();
            // PrintJobInfo();
            PrintQueue();

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
                return  new RoslynClient(values[0], values[1]);
            }
            catch
            {
                return new RoslynClient();
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

}
