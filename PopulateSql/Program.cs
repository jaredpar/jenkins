using Roslyn.Jenkins;
using Roslyn.Sql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PopulateSql
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            var connectionString = File.ReadAllText(@"c:\users\jaredpar\connection.txt").Trim();

            TestJenkinsTestRunDateTimes(connectionString);
            // TestBuildSource(connectionString);
            // var client = new DataClient(connectionString);
            // PopulateAllJobInfos(client);
            // PopulateAllFailures(client);
            // PopulateAllRetest(client);
        }

        private static void TestBuildSource(string connectionString)
        {
            using (var util = new SqlUtil(connectionString))
            {
                var id1 = util.GetBuildSourceId("jaredpar06", @"e:\dd\roslyn");
                var id2 = util.GetBuildSourceId("jaredpar06", @"e:\dd\roslyn");
                Console.WriteLine($"{id1} = {id2}");
            }

        }

        private static void TestJenkinsTestRunDateTimes(string connectionString)
        {
            using (var util = new SqlUtil(connectionString))
            {
                var list = util.GetJenkinsTestRunDateTimes();
                var all = list
                    .Select(NormalizeDateTime)
                    .GroupBy(x => x)
                    .OrderBy(x => x.Key);
                foreach (var cur in all)
                {
                    Console.WriteLine($"{cur.Key} -> {cur.Count()}");
                }
            }
        }

        private static TimeSpan NormalizeDateTime(DateTime utc)
        {
            var local = utc.ToLocalTime();
            int minutes;
            if (utc.Minute < 30)
            {
                minutes = 0;
            }
            else
            {
                minutes = 30;
            }

            return new TimeSpan(hours: local.Hour, minutes: minutes, seconds: 0);
        }

        /*
        private static void PopulateAllJobInfos(DataClient dataClient)
        {
            var roslynClient = new RoslynClient();
            var client = roslynClient.Client;
            foreach (var name in roslynClient.GetJobNames())
            {
                List<BuildId> jobs;
                try
                {
                    jobs = client.GetBuildIds(name);
                }
                catch
                {
                    Console.WriteLine($"Can't get jobs for {name}");
                    continue;
                }

                foreach (var id in jobs)
                {
                    try
                    {
                        Console.Write($"Processing {id.Id} {id.Name} ... ");
                        var info = client.GetBuildInfo(id);
                        dataClient.InsertJobInfo(info);
                        Console.WriteLine("Done");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR!!!");
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private static void PopulateAllFailures(DataClient dataClient)
        {
            /*
            var client = new JenkinsClient();
            foreach (var id in client.GetJobIds())
            {
                try
                {
                    Console.Write($"Processing {id.Id} {id.Kind} ... ");
                    var jobResult = client.GetJobResult(id);
                    if (!jobResult.Failed)
                    {
                        Console.WriteLine("Succeeded");
                        continue;
                    }

                    dataClient.InsertFailure(jobResult.JobInfo, jobResult.FailureInfo);

                    Console.WriteLine("Done");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR!!!");
                    Console.WriteLine(ex.Message);
                }


            }
        }

        private static void PopulateAllRetest(DataClient dataClient)
        {
            var list = dataClient.GetFailures();
            foreach (var tuple in list)
            {
                var id = tuple.Item1;
                var sha = tuple.Item2;
                if (dataClient.HasSucceeded(id.Kind, sha))
                {
                    Console.WriteLine(id);
                    dataClient.InsertRetest(id, sha);
                }
            }
        }
            */
    }
}
