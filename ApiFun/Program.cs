using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dashboard.Jenkins;
using System.Diagnostics;
using System.IO;
using RestSharp.Authenticators;
using System.Configuration;
using Dashboard.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Globalization;
using Newtonsoft.Json;
using static Dashboard.Azure.AzureConstants;
using System.Threading;
using Microsoft.WindowsAzure.Storage.Queue;
using Dashboard.StorageBuilder;

namespace Dashboard.ApiFun
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            // JenkinsDataUtil.Go();
            // FillData().Wait();
            // PrintMacTimes();
            // var util = new MachineCountInvestigation(CreateClient());
            // util.Go();
            // GetMacQueueTimes();
            // TestJob().Wait();
            TestFailure().Wait();
            //WriteJobList().Wait();
            // Test().Wait();
            // DrainQueue().Wait();
            // OomTest();
            //DrainPoisonQueue().Wait();
            // CheckUnknown().Wait();
            // Random().Wait();
            // ViewNameMigration().Wait();
            // TestPopulator().Wait();
            // MigrateCounter().Wait();
            // FindRetest();
            // PrintRetestInfo();
            // InspectReason(5567);
            // ScanAllFailedJobs();
            // PrintJobNames();
            // PrintJobInfo();
            // PrintQueue();
            // PrintViews();
            //PrintPullRequestData();
            // PrintFailure();
            // PrintJobs();

            /*
            roslyn_stabil_lin_dbg_unit32
            */
            // Migrate().Wait();
        }

        /*
        internal static async Task Iterate()
        {
            const string root = @"c:\users\jaredpar\temp\data\builds";
            var client 

        }
        */

        private static async Task FillData()
        {
            var account = GetStorageAccount();
            var table = account.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.BuildResultDate);
            var list = new List<BuildResultEntity>();
            var query = new TableQuery<BuildResultEntity>().Where("PartitionKey gt '00000335'");
            foreach (var entity in table.ExecuteQuery(query))
            {
                entity.ViewName = AzureUtil.GetViewName(entity.JobId);
                list.Add(entity);
            }

            await AzureUtil.InsertBatchUnordered(table, list);
        }

        private static async Task TestJob()
        {
            var jobUrlStr = "http://dotnet-ci.cloudapp.net/job/Private/job/dotnet_roslyn-internal/job/microupdate/job/windows_vsi_p2/8/";
            var uri = new Uri(jobUrlStr);
            var parts = uri.PathAndQuery.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var jobPath = string.Join("/", parts.Take(parts.Length - 1));
            var number = int.Parse(parts.Last());
            var jobId = JenkinsUtil.ConvertPathToJobId(jobPath);
            var buildId = new BuildId(number, jobId);

            var account = GetStorageAccount();
            var populator = new BuildTablePopulator(account.CreateCloudTableClient(), CreateClient(SharedConstants.DotnetJenkinsHostName), Console.Out);
            await populator.PopulateBuild(buildId);

        }

        private static async Task DrainPoisonBuildQueue()
        {
            var account = GetStorageAccount();
            var client = account.CreateCloudQueueClient();
            var queue = client.GetQueueReference($"{AzureConstants.QueueNames.BuildEvent}-poison");
            var populator = new BuildTablePopulator(account.CreateCloudTableClient(), CreateClient(SharedConstants.DotnetJenkinsHostName), Console.Out);
            var set = new HashSet<BuildId>();
            do
            {
                var message = await queue.GetMessageAsync();
                var obj = JObject.Parse(message.AsString);
                var jobPath = obj.Value<string>("jobName");
                var number = obj.Value<int>("number");
                var buildId = new BuildId(number, JenkinsUtil.ConvertPathToJobId(jobPath));
                if (!set.Add(buildId))
                {
                    continue;
                }

                await populator.PopulateBuild(buildId);
                await queue.DeleteMessageAsync(message);
            } while (true);
        }

        private static async Task DrainQueue()
        {
            var item = new BuildDrainer();
            await item.Go();
        }

        /*
        private static async Task PopulateJobFailureTable()
        {
            var tableConnectionString = ConfigurationManager.AppSettings[SharedConstants.StorageConnectionStringName];
            var storageAccount = CloudStorageAccount.Parse(tableConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var buildProcessedTable = tableClient.GetTableReference(BuildProcessedEntity.TableName);

            var list = new List<BuildResultDateEntity>();
            var query = new TableQuery<BuildProcessedEntity>();
            TableContinuationToken token = null;
            do
            {
                var segment = await buildProcessedTable.ExecuteQuerySegmentedAsync(query, token);
                foreach (var entity in segment.Results)
                {
                    var resultEntity = new BuildResultDateEntity(
                        new DateTimeOffset(entity.BuildDate),
                        entity.BuildId,
                        entity.MachineName,
                        entity.Kind);
                    list.Add(resultEntity);
                }

                token = segment.ContinuationToken;
            } while (token != null);

            var buildResultTable = tableClient.GetTableReference(BuildResultDateEntity.TableName);
            buildResultTable.CreateIfNotExists();
            await AzureUtil.InsertBatchUnordered(buildResultTable, list);
        }
        */

        internal static JenkinsClient CreateClient(string hostName, JobId jobId = null, bool? auth = null)
        {
            if (auth == null && jobId != null)
            {
                auth = JobUtil.IsAuthNeededHeuristic(jobId);
            }

            var builder = new UriBuilder();
            builder.Host = hostName;
            builder.Scheme = auth == true ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;

            if (auth == true)
            {
                var text = ConfigurationManager.AppSettings[SharedConstants.GithubConnectionStringName];
                return new JenkinsClient(builder.Uri, text);
            }

            return new JenkinsClient(builder.Uri);
        }

        private static JenkinsClient CreateClient(BoundBuildId buildId, bool? auth = null)
        {
            return CreateClient(buildId.HostName, buildId.JobId, auth);
        }

        private static void Authorize(RestRequest request)
        {
            var text = ConfigurationManager.AppSettings[SharedConstants.GithubConnectionStringName];
            var values = text.Split(':');
            SharedUtil.AddAuthorization(request, values[0], values[1]);
        }

        internal static CloudStorageAccount GetStorageAccount()
        {
            var tableConnectionString = ConfigurationManager.AppSettings[SharedConstants.StorageConnectionStringName];
            var storageAccount = CloudStorageAccount.Parse(tableConnectionString);
            return storageAccount;
        }

        private static void AddAuthentication(RestRequest request)
        {
            var text = ConfigurationManager.AppSettings[SharedConstants.GithubConnectionStringName];
            var items = text.Split(new[] { ':' }, count: 2);
            var bytes = Encoding.UTF8.GetBytes($"{items[0]}:{items[1]}");
            var encoded = Convert.ToBase64String(bytes);
            var header = $"Basic {encoded}";
            request.AddHeader("Authorization", header);
        }

        private static async Task MigrateCounter()
        {
            var account = GetStorageAccount();
            var tableClient = account.CreateCloudTableClient();
            var tableNames = new[]
            {
                AzureConstants.TableNames.TestCacheCounter,
                AzureConstants.TableNames.TestRunCounter,
                AzureConstants.TableNames.UnitTestQueryCounter
            };
            foreach (var tableName in tableNames)
            {
                var table = tableClient.GetTableReference(tableName);
                var query = new TableQuery<DynamicTableEntity>().Select(new[] { "PartitionKey", "RowKey" });
                var list = new List<DynamicTableEntity>();
                foreach (var entity in table.ExecuteQuery(query))
                {
                    DateTime dateTime;
                    if (!DateTime.TryParseExact(entity.PartitionKey, "yyyy-MM-dd", CultureInfo.CurrentCulture, DateTimeStyles.None, out dateTime))
                    {
                        continue;
                    }

                    list.Add(entity);
                }

                await AzureUtil.DeleteBatchUnordered(table, list);
            }

        }

        private static void OomTest()
        {
            var buildId = new BuildId(352, JobId.ParseName("dotnet_corefx/master/windows_nt_release_prtest"));
            var client = CreateClient(SharedConstants.DotnetJenkinsHostName);
            var list = client.GetFailedTestCases(buildId);
            Console.WriteLine(list.Count);
        }

        private static async Task Random()
        {
            /*
            var boundBuildId = BoundBuildId.Parse("https://dotnet-ci.cloudapp.net/job/dotnet_corefx/job/master/job/fedora23_debug_tst/134/");
            var buildId = boundBuildId.BuildId;
            var client = CreateClient(uri: boundBuildId.HostUri, auth: true);
            var buildInfo = await client.GetBuildInfoAsync(buildId);
            var buildResult = await client.GetBuildResultAsync(buildInfo);
            var test = await client.GetFailedTestCasesAsync(buildId);
            var prInfo = await client.GetPullRequestInfoAsync(buildId);
            */

            var testboundBuildId = BoundBuildId.Parse("https://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_1.0.0/job/x64_release_rhel7.2_pri1_flow/30/");
            var testbuildId = testboundBuildId.BuildId;
            var client = CreateClient(testboundBuildId);
            var elapsedTimeObj = client.GetBuildInfo(testbuildId).Duration;
            Console.WriteLine($"\tET: {elapsedTimeObj.TotalMilliseconds}");

            var account = GetStorageAccount();
            var dateKey = new DateKey(DateTimeOffset.UtcNow - TimeSpan.FromDays(1));
            var table = account.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.BuildResultDate);
            var query = new TableQuery<BuildResultEntity>()
                .Where(FilterUtil
                    .Column(ColumnNames.PartitionKey, dateKey, ColumnOperator.GreaterThanOrEqual)
                    .And(FilterUtil.Column("MachineName", "Azure0602081822")));
            var all = await AzureUtil.QueryAsync(table, query);
            foreach (var entity in all)
            {
                var boundBuildId = new BoundBuildId(SharedConstants.DotnetJenkinsUri.Host, entity.BuildId);
                Console.WriteLine(boundBuildId.GetBuildUri(useHttps: false));
            }
        }

        /// <summary>
        /// Function to help diagnose failures in processing.
        /// </summary>
        /// <returns></returns>
        private static async Task TestFailure()
        {
            var failureUrl = @"https://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/jitstress/job/x64_checked_ubuntu_minopts_flow/13/";
            var boundBuildId = BoundBuildId.Parse(failureUrl);
            var buildId = boundBuildId.BuildId;
            var account = GetStorageAccount();
            var client = CreateClient(boundBuildId);
            var populator = new BuildTablePopulator(account.CreateCloudTableClient(), client, Console.Out);

            try
            {
                await populator.PopulateBuild(buildId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task Test()
        {
            var account = GetStorageAccount();
            var tableClient = account.CreateCloudTableClient();
            var table = tableClient.GetTableReference(AzureConstants.TableNames.BuildResultDate);
            var query = new TableQuery<DynamicTableEntity>()
                .Where(FilterUtil.SinceDate(ColumnNames.PartitionKey, DateTimeOffset.UtcNow - TimeSpan.FromDays(2)))
                .Select(new[] { "JobName" });
            var nameSet = new HashSet<string>();
            var kindSet = new HashSet<string>();
            foreach (var entity in table.ExecuteQuery(query))
            {
                var name = entity["JobName"].StringValue;
                if (nameSet.Add(name))
                {
                    var id = JobId.ParseName(name);
                    try
                    {
                        var client = CreateClient(SharedConstants.DotnetJenkinsHostName, id);
                        var kind = await client.GetJobKindAsync(id);
                        if (kindSet.Add(kind))
                        {
                            Console.WriteLine(kind);
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }
        }

        private static async Task ViewNameMigration()
        {
            var account = GetStorageAccount();
            var client = account.CreateCloudTableClient();
            var viewNameTable = client.GetTableReference(AzureConstants.TableNames.ViewNameDate);
            var buildResultTable = client.GetTableReference(AzureConstants.TableNames.BuildResultDate);
            var startDate = DateTimeOffset.UtcNow - TimeSpan.FromDays(14);

            var query = new TableQuery<DynamicTableEntity>()
                .Where(FilterUtil.SinceDate(ColumnNames.PartitionKey, startDate))
                .Select(new[] { "PartitionKey", nameof(BuildResultEntity.ViewName) });
            var all = await AzureUtil.QueryAsync(buildResultTable, query);
            var set = new HashSet<Tuple<DateKey, string>>();
            var list = new List<ViewNameEntity>();
            foreach (var entity in all)
            {
                var dateKey = DateKey.Parse(entity.PartitionKey);
                var viewName = entity.Properties[nameof(BuildResultEntity.ViewName)].StringValue;
                var tuple = Tuple.Create(dateKey, viewName);
                if (set.Add(tuple))
                {
                    list.Add(new ViewNameEntity(dateKey, viewName));
                }
            }

            await AzureUtil.InsertBatchUnordered(viewNameTable, list);
        }

        private static async Task TestPopulator()
        {
            var account = GetStorageAccount();
            var client = CreateClient(SharedConstants.DotnetJenkinsHostName, auth: false);
            var populator = new BuildTablePopulator(account.CreateCloudTableClient(), client, Console.Out);

            var boundBuildId = BoundBuildId.Parse("https://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/jitstress/job/x64_checked_osx_jitstress1_flow/7/");
            try
            {
                await populator.PopulateBuild(boundBuildId.BuildId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task CheckUnknown()
        {
            var account = GetStorageAccount();
            var buildUtil = new BuildUtil(account);
            var date = DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            var populator = new BuildTablePopulator(account.CreateCloudTableClient(), CreateClient(SharedConstants.DotnetJenkinsHostName), Console.Out);
            var table = account.CreateCloudTableClient().GetTableReference(AzureConstants.TableNames.BuildResultDate);
            foreach (var entity in buildUtil.GetBuildResultsByKindName(date, BuildResultClassification.Unknown.Name, AzureUtil.ViewNameAll))
            {
                var entityDate = DateKey.Parse(entity.PartitionKey);
                var before = new DateKey(entityDate.Date.AddDays(-1));
                var after = new DateKey(entityDate.Date.AddDays(1));

                var op = TableOperation.Retrieve(before.Key, entity.RowKey);
                var result = await table.ExecuteAsync(op);
                if (result.Result != null)
                {
                    await table.ExecuteAsync(TableOperation.Delete(entity));
                    continue;
                }

                op = TableOperation.Retrieve(after.Key, entity.RowKey);
                result = await table.ExecuteAsync(op);
                if (result.Result != null)
                {
                    await table.ExecuteAsync(TableOperation.Delete(entity));
                    continue;
                }
            }
        }

        private static void PrintFailure()
        {
            var client = CreateClient(SharedConstants.DotnetJenkinsHostName, auth: false);
            var info = client.GetBuildFailureInfo(new BuildId(number: 6066, jobId: JobId.ParseName("roslyn_prtest_win_dbg_unit64")));
            // Console.WriteLine(info.Category);
        }

        private static void PrintViews()
        {
            foreach (var viewInfo in CreateClient(SharedConstants.DotnetJenkinsHostName).GetViews())
            {
                Console.WriteLine($"{viewInfo.Name} {viewInfo.Url}");
            }
        }

        private static void PrintQueue()
        {
            var client = CreateClient(SharedConstants.DotnetJenkinsHostName);
            foreach (var cur in client.GetQueuedItemInfoList())
            {
                Console.WriteLine($"{cur.JobId} {cur.Id} {cur.PullRequestInfo?.PullUrl ?? ""}");
            }
        }

        private static void PrintPullRequestData()
        {
            var client = CreateClient(SharedConstants.DotnetJenkinsHostName);
            var list = new List<Tuple<DateTime, BuildId>>();
            foreach (var jobId in client.GetJobIdsInView("Roslyn").Where(x => x.Name.Contains("prtest")).Where(x => !x.Name.Contains("internal")))
            {
                Console.WriteLine($"Job: {jobId}");
                foreach (var buildId in client.GetBuildIds(jobId))
                {
                    Console.WriteLine($"\tBuild: {buildId.Number}");
                    var date = client.GetBuildInfo(buildId).Date;
                    var elapsedTime = client.GetBuildInfo(buildId).Duration;
                    // list.Add(Tuple.Create(date, buildId));
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
            var client = CreateClient(SharedConstants.DotnetJenkinsHostName);
            var miniTimes = new List<TimeSpan>();
            var proTimes = new List<TimeSpan>();

            foreach (var buildId in client.GetBuildIds(new JobId("roslyn_prtest_mac_dbg_unit32", JobId.Root)))
            {
                var buildInfo = client.GetBuildInfo(buildId);
                var result = client.GetBuildResult(buildInfo);
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
            var client = CreateClient(SharedConstants.DotnetJenkinsHostName);
            foreach (var buildId in client.GetBuildIds(new JobId("roslyn_prtest_mac_dbg_unit32", JobId.Root)))
            {
                Console.WriteLine($"Processing {buildId.Number}");

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
                    Console.WriteLine($"Could not get duration for {buildId.Number}");
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
            var client = CreateClient(SharedConstants.DotnetJenkinsHostName);
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
                return OS.FreeBSD;
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
                var buildInfo = _client.GetBuildInfo(buildId);
                var result = _client.GetBuildResult(buildInfo);
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
                var buildInfo = _client.GetBuildInfo(buildId);
                var result = _client.GetBuildResult(buildInfo);
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
                Console.WriteLine($"Processing {buildId.JobName} {buildId.Number}");

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

    internal sealed class BuildDrainer
    {
        private enum State
        {
            Succeeded,
            Failed,
            Skipped
        }
        private struct Result
        {
            internal BuildId BuildId { get; }
            internal CloudQueueMessage Message { get; }
            internal State State { get; set; }
            internal TimeSpan Time { get; set; }
            internal string Error { get; set; }

            internal Result(BuildId id, CloudQueueMessage message, State state)
            {
                this = default(Result);
                BuildId = id;
                Message = message;
                State = state;
            }
        }

        internal async Task Go()
        {
            var succeeded = 0;
            var failed = 0;
            var skipped = 0;
            var maxCount = 10;
            var list = new List<Task<Result>>(capacity: maxCount);
            var account = Program.GetStorageAccount();
            var client = account.CreateCloudQueueClient();
            var queue = client.GetQueueReference(AzureConstants.QueueNames.ProcessBuild);
            var queueDone = false;
            var set = new HashSet<BuildId>();

            do
            {
                while (list.Count < maxCount && !queueDone)
                {
                    var message = await queue.GetMessageAsync();
                    if (message == null)
                    {
                        queueDone = true;
                        break;
                    }

                    var buildIdJson = JsonConvert.DeserializeObject<BuildIdJson>(message.AsString);
                    var buildId = buildIdJson.BuildId;
                    if (!set.Add(buildId))
                    {
                        await queue.DeleteMessageAsync(message);
                        continue;
                    }

                    var task = Task.Run(() => Go(buildId, message));
                    list.Add(task);
                    Console.WriteLine($"Queued {buildId}");
                }

                Console.WriteLine($"Succeeded {succeeded} Failed {failed} Skipped {skipped}");
                Task.WaitAny(list.ToArray());

                var index = 0;
                while (index < list.Count)
                {
                    var task = list[index];
                    if (!task.IsCompleted)
                    {
                        index++;
                        continue;
                    }

                    list.RemoveAt(index);
                    var result = await task;
                    switch (result.State)
                    {
                        case State.Succeeded:
                            Console.WriteLine($"{result.BuildId} succeeded in {result.Time}");
                            succeeded++;
                            break;
                        case State.Skipped:
                            Console.WriteLine($"{result.BuildId} skipped");
                            skipped++;
                            break;
                        case State.Failed:
                            Console.WriteLine($"{result.BuildId} failed");
                            Console.WriteLine(result.Error);
                            failed++;
                            break;
                    }

                    await queue.DeleteMessageAsync(result.Message);

                    task.Dispose();
                }
            } while (true);
        }


        private async Task<Result> Go(BuildId buildId, CloudQueueMessage message)
        {
            var account = Program.GetStorageAccount();
            var client = Program.CreateClient(SharedConstants.DotnetJenkinsHostName, buildId.JobId);
            var queueClient = account.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(QueueNames.ProcessBuild);
            var populator = new BuildTablePopulator(account.CreateCloudTableClient(), client, TextWriter.Null);

            /*
            // There is too much of a back log to every fully process the corefx logs that are 200MB+ in size.  Skip them
            // until we hit the recent ones. 
            if (buildId.JobName.Contains("corefx"))
            {
                try
                {
                    var info = await client.GetBuildInfoAsync(buildId);
                    if (info.Date < DateTimeOffset.UtcNow.AddHours(-1))
                    {
                        return new Result(buildId, message, State.Succeeded);
                    }
                }
                catch (Exception ex)
                {
                    return new Result(buildId, message, State.Failed) { Error = ex.ToString() };
                }
            }
            */

            if (await populator.IsPopulated(buildId))
            {
                return new Result(buildId, message, State.Skipped);
            }

            try
            {
                var start = DateTimeOffset.Now;
                await populator.PopulateBuild(buildId);
                var span = DateTimeOffset.Now - start;
                return new Result(buildId, message, State.Succeeded) { Time = span };
            }
            catch (Exception ex)
            {
                return new Result(buildId, message, State.Failed) { Error = ex.ToString() };
            }
        }
    }

}


