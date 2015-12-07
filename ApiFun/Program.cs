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
            // Random();
            // FindRetest();
            // PrintRetestInfo();
            // InspectReason(5567);
            // ScanAllFailedJobs();
            PrintJobNames();
            // PrintJobInfo();

            /*
            roslyn_stabil_lin_dbg_unit32
            */
        }

        private static RoslynClient CreateClient()
        {
            try
            {
                var text = File.ReadAllText(@"c:\users\jaredpar\jenkins.txt").Trim();
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
                foreach (var id in client.GetJobIds(name))
                {
                    try
                    {
                        var info = client.GetJobInfo(id);
                        Console.WriteLine($"\t{id} {info.Sha.Substring(0, 7)} {info.State}");
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
            var client = new JenkinsClient();
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
            foreach (var jobId in client.GetJobIds(JobKind.WindowsDebug32))
            {
                Console.Write($"{jobId} ");
                try
                {
                    var jobResult = client.GetJobResult(jobId);
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
            var client = new JenkinsClient();
            var names = client.GetJobNamesInView("roslyn");
            var jobIdList = client.GetJobIds(names.ToArray());

            foreach (var cur in jobIdList)
            {
                var jobResult = client.GetJobResult(cur);
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
