using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public sealed class JenkinsClient
    {
        private readonly Uri _baseUrl;
        private readonly RestClient _restClient;
        private readonly string _authorizationHeaderValue;

        public RestClient RestClient => _restClient;

        public JenkinsClient(Uri baseUrl)
        {
            _baseUrl = baseUrl;
            _restClient = new RestClient(baseUrl);
        }

        public JenkinsClient(Uri baseUrl, string username, string password) 
            : this(baseUrl)
        {
            var bytes = Encoding.UTF8.GetBytes($"{username}:{password}");
            var encoded = Convert.ToBase64String(bytes);
            _authorizationHeaderValue = $"Basic {encoded}";
        }

        /// <summary>
        /// Get all of the available job names
        /// </summary>
        /// <returns></returns>
        public List<string> GetJobNames()
        {
            var data = GetJson("");
            return GetJobNamesCore(data);
        }

        public List<string> GetJobNamesInView(string viewName)
        {
            var data = GetJson($"/view/{viewName}/");
            return GetJobNamesCore(data);
        }

        private List<string> GetJobNamesCore(JObject data)
        {
            var jobs = (JArray)data["jobs"];
            var list = new List<string>();
            foreach (var cur in jobs)
            {
                var name = cur.Value<string>("name");
                list.Add(name);
            }

            return list;
        }

        public List<BuildId> GetBuildIds()
        {
            var all = GetJobNames().ToArray();
            return GetBuildIds(all);
        }

        public List<BuildId> GetBuildIds(string jobName)
        {
            var data = GetJson($"job/{jobName}/");
            var all = (JArray)data["builds"] ?? new JArray();
            var list = new List<BuildId>();

            foreach (var cur in all)
            {
                var build = cur.ToObject<Json.Build>();
                list.Add(new BuildId(build.Number, jobName));
            }

            return list;
        }

        public List<BuildId> GetBuildIds(params string[] jobNames)
        {
            var list = new List<BuildId>();
            foreach (var name in jobNames)
            {
                list.AddRange(GetBuildIds(name));
            }

            return list;
        }

        public List<BuildInfo> GetBuildInfo(string jobName)
        {
            var json = GetJson(
                JenkinsUtil.GetJobPath(jobName),
                tree: "builds[result,id,duration,timestamp]",
                depth: 2);
            var list = new List<BuildInfo>();
            var builds = (JArray)json["builds"];
            foreach (JObject data in builds)
            {
                var id = data.Value<int>("id");
                var duration = TimeSpan.FromMilliseconds(data.Value<int>("duration"));
                var state = GetBuildStateCore(data);
                var date = GetBuildDateCore(data);

                list.Add(new BuildInfo(new BuildId(id, jobName), state, date, duration));
            }

            return list;
        }

        public BuildInfo GetBuildInfo(BuildId id)
        {
            var data = GetJson(id);
            var state = GetBuildStateCore(data);
            var date = GetBuildDateCore(data);
            var duration = TimeSpan.FromMilliseconds(data.Value<int>("duration"));
            return new BuildInfo(id, state, date, duration);
        }

        public BuildResult GetBuildResult(BuildId id)
        {
            var data = GetJson(id);
            var state = GetBuildStateCore(data);
            var date = GetBuildDateCore(data);
            var jobInfo = new BuildInfo(
                id,
                state,
                date,
                TimeSpan.FromMilliseconds(data.Value<int>("duration")));

            if (state == BuildState.Failed)
            {
                var failureInfo = GetBuildFailureInfo(id, data);
                return new BuildResult(jobInfo, failureInfo);
            }

            return new BuildResult(jobInfo);
        }

        public BuildState GetBuildState(BuildId id)
        {
            var data = GetJson(id, tree: "result");
            return GetBuildStateCore(data);
        }

        /// <summary>
        /// Get all of the queued items in Jenkins
        /// </summary>
        /// <returns></returns>
        public List<QueuedItemInfo> GetQueuedItemInfo()
        {
            var data = GetJson("queue");
            var items = (JArray)data["items"];
            var list = new List<QueuedItemInfo>();
            foreach (var item in items)
            {
                PullRequestInfo prInfo;
                JsonUtil.TryParsePullRequestInfo((JArray)item["actions"], out prInfo);
                var id = item.Value<int>("id");
                var jobName = item["task"].Value<string>("name");
                list.Add(new QueuedItemInfo(id, jobName, prInfo));
            }

            return list;
        }

        public List<ViewInfo> GetViews()
        {
            var list = new List<ViewInfo>();
            var data = GetJson("", tree: "views[*]");
            var items = (JArray)data["views"];

            foreach (JObject pair in items)
            {
                var name = pair.Value<string>("name");
                var description = pair.Value<string>("description");
                var url = new Uri(pair.Value<string>("url"));
                list.Add(new ViewInfo(name, description, url));
            }

            return list;
        }

        public List<ComputerInfo> GetComputerInfo()
        {
            var list = new List<ComputerInfo>();
            var data = GetJson("computer", tree: "computer[*]");
            var items = (JArray)data["computer"];

            foreach (JObject item in items)
            {
                var name = item.Value<string>("displayName");
                var os = item["monitorData"].Value<string>("hudson.node_monitors.ArchitectureMonitor");
                list.Add(new ComputerInfo(name, os));
            }

            return list;
        }

        private DateTime GetBuildDateCore(JObject data)
        {
            var seconds = data.Value<long>("timestamp");
            var epoch = new DateTime(year: 1970, month: 1, day: 1);
            return epoch.AddMilliseconds(seconds);
        }

        private BuildState GetBuildStateCore(JObject data)
        {
            var result = data.Property("result");
            if (result == null)
            {
                throw new Exception("Could not find the result property");
            }

            BuildState? state = null;
            switch (result.Value.Value<string>())
            {
                case "SUCCESS":
                    state = BuildState.Succeeded;
                    break;
                case "FAILURE":
                    state = BuildState.Failed;
                    break;
                case "ABORTED":
                    state = BuildState.Aborted;
                    break;
                case null:
                    state = BuildState.Running;
                    break;
            }

            if (state == null)
            {
                throw new Exception("Unable to determine the success / failure of the job");
            }

            return state.Value;
        }

        public PullRequestInfo GetPullRequestInfo(BuildId id)
        {
            var data = GetJson(id);
            return GetPullRequestInfoCore(id, data);
        }

        private PullRequestInfo GetPullRequestInfoCore(BuildId id, JObject data)
        {
            var actions = (JArray)data["actions"];
            return JsonUtil.ParsePullRequestInfo(actions);
        }

        private string GetSha1Core(JObject data)
        {
            var actions = (JArray)data["actions"];
            foreach (var item in actions)
            {
                var obj = item["lastBuiltRevision"];
                if (obj != null)
                {
                    return obj.Value<string>("SHA1");
                }
            }

            PullRequestInfo info;
            if (JsonUtil.TryParsePullRequestInfo(actions, out info))
            {
                return info.Sha1;
            }

            throw new Exception("Can't read sha1");
        }

        public string GetConsoleText(BuildId id)
        {
            var uri = JenkinsUtil.GetConsoleTextUri(_baseUrl, id);
            var request = WebRequest.Create(uri);
            using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public JObject GetJson(string urlPath, bool pretty = false, string tree = null, int? depth = null)
        {
            urlPath = urlPath.TrimEnd('/');
            var request = new RestRequest($"{urlPath}/api/json", Method.GET);
            request.AddParameter("pretty", pretty ? "true" : "false");

            if (depth.HasValue)
            {
                request.AddParameter("depth", depth);
            }

            if (!string.IsNullOrEmpty(tree))
            {
                request.AddParameter("tree", tree);
            }

            if (!string.IsNullOrEmpty(_authorizationHeaderValue))
            {
                request.AddHeader("Authorization", _authorizationHeaderValue);
            }

            var response = _restClient.Execute(request);
            return JObject.Parse(response.Content);
        }

        private JObject GetJson(BuildId buildId, bool pretty = false, string tree = null, int? depth = null)
        {
            var path = JenkinsUtil.GetBuildPath(buildId);
            return GetJson(path, pretty, tree, depth);
        }

        /// <summary>
        /// Attempt to determine the failure reason for the given Job.  This should  only be called on 
        /// jobs that are known to have failed.
        /// </summary>
        private GetBuildFailureInfo GetBuildFailureInfo(BuildId buildId, JObject data)
        {
            // First look for the test failure information.  
            List<string> failedTestList;
            if (TryGetTestFailureReason(buildId, data, out failedTestList))
            {
                Debug.Assert(failedTestList.Count > 0);
                return new GetBuildFailureInfo(BuildFailureReason.TestCase, failedTestList);
            }

            // Now look at the console text.
            var consoleText = GetConsoleText(buildId);
            GetBuildFailureInfo failureInfo;
            if (ConsoleTextUtil.TryGetFailureInfo(consoleText, out failureInfo))
            {
                return failureInfo;
            }

            return Jenkins.GetBuildFailureInfo.Unknown;
        }

        // TODO: This should be in JenkinsUtil
        private bool TryGetTestFailureReason(BuildId buildId, JObject data, out List<string> failedTestList)
        {
            var actions = (JArray)data["actions"];
            foreach (var cur in actions)
            {
                var failCount = cur.Value<int?>("failCount");
                if (failCount != null && failCount.Value != 0)
                {
                    var testReportUrl = cur.Value<string>("urlName");
                    var path = $"{JenkinsUtil.GetBuildPath(buildId)}{testReportUrl}/";
                    failedTestList = GetFailedTests(path);
                    return true;
                }
            }

            failedTestList = null;
            return false;
        }

        /// <summary>
        /// Get the list of failed test names from the specified test report URL
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private List<string> GetFailedTests(string testReportUrlPath)
        {
            var list = new List<string>();
            var data = GetJson(testReportUrlPath);
            var suites = (JArray)data["suites"];
            foreach (var suite in suites)
            {
                var cases = (JArray)suite["cases"];
                foreach (var cur in cases)
                {
                    var status = cur.Value<string>("status");
                    if (status == "PASSED" || status == "SKIPPED" || status == "FIXED")
                    {
                        continue;
                    }

                    var className = cur.Value<string>("className");
                    var name = cur.Value<string>("name");
                    list.Add($"{className}.{name}");
                }
            }

            return list;
        }
    }
}
