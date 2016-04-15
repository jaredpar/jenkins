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

namespace Dashboard.Jenkins
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

        public JenkinsClient(Uri baseUrl, string connectionString)
            : this(baseUrl)
        {
            var items = connectionString.Split(new[] { ':' }, count: 2);
            var bytes = Encoding.UTF8.GetBytes($"{items[0]}:{items[1]}");
            var encoded = Convert.ToBase64String(bytes);
            _authorizationHeaderValue = $"Basic {encoded}";
        }

        public JenkinsClient(Uri baseUrl, string username, string password) 
            : this(baseUrl, $"{username}:{password}")
        {

        }

        /// <summary>
        /// Get all of the <see cref="JobId"/> values that are children of the provided
        /// value.  The provided value can be a folder, the root or even a normal job. The
        /// latter of which simply will not have any children.
        /// </summary>
        public List<JobId> GetJobIds(JobId parent = null)
        {
            parent = parent ?? JobId.Root;
            var data = GetJson(JenkinsUtil.GetJobIdPath(parent));
            return JsonUtil.ParseJobs(parent, (JArray)data["jobs"]);
        }

        public async Task<List<JobId>> GetJobIdsAsync(JobId parent = null)
        {
            parent = parent ?? JobId.Root;
            var data = await GetJsonAsync(JenkinsUtil.GetJobIdPath(parent));
            return JsonUtil.ParseJobs(parent, (JArray)data["jobs"]);
        }

        public List<JobId> GetJobIdsInView(string viewName)
        {
            var data = GetJson($"/view/{viewName}/");
            return JsonUtil.ParseJobs(JobId.Root, (JArray)data["jobs"]);
        }

        public async Task<List<JobId>> GetJobIdsInViewAsync(string viewName)
        {
            var data = await GetJsonAsync($"/view/{viewName}/");
            return JsonUtil.ParseJobs(JobId.Root, (JArray)data["jobs"]);
        }

        public List<BuildId> GetBuildIds(JobId jobId)
        {
            var data = GetJson(JenkinsUtil.GetJobIdPath(jobId));
            return JsonUtil.ParseBuilds(jobId, (JArray)data["jobs"] ?? new JArray());
        }

        public async Task<List<BuildId>> GetBuildIdsAsync(JobId jobId)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetJobIdPath(jobId));
            return JsonUtil.ParseBuilds(jobId, (JArray)data["jobs"] ?? new JArray());
        }

        public BuildInfo GetBuildInfo(BuildId id)
        {
            var data = GetJson(JenkinsUtil.GetBuildPath(id), tree: JsonUtil.BuildInfoTreeFilter);
            return JsonUtil.ParseBuildInfo(id.JobId, data);
        }

        public async Task<BuildInfo> GetBuildInfoAsync(BuildId id)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetBuildPath(id), tree: JsonUtil.BuildInfoTreeFilter);
            return JsonUtil.ParseBuildInfo(id.JobId, data);
        }

        /// <summary>
        /// Get all of the <see cref="BuildInfo"/> values for the specified <see cref="JobId"/>.
        /// </summary>
        public List<BuildInfo> GetBuildInfoList(JobId id)
        {
            var data = GetJson(JenkinsUtil.GetJobIdPath(id), tree: JsonUtil.BuildInfoListTreeFilter, depth: 2);
            return JsonUtil.ParseBuildInfoList(id, data);
        }

        public async Task<List<BuildInfo>> GetBuildInfoListAsync(JobId id)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetJobIdPath(id), tree: JsonUtil.BuildInfoListTreeFilter, depth: 2);
            return JsonUtil.ParseBuildInfoList(id, data);
        }

        public JobInfo GetJobInfo(JobId id)
        {
            var json = GetJson(JenkinsUtil.GetJobIdPath(id));
            return JsonUtil.ParseJobInfo(id, json);
        }

        public async Task<JobInfo> GetJobInfoAsync(JobId id)
        {
            var json = await GetJsonAsync(JenkinsUtil.GetJobIdPath(id));
            return JsonUtil.ParseJobInfo(id, json);
        }

        public BuildResult GetBuildResult(BuildId id)
        {
            var data = GetJson(JenkinsUtil.GetBuildPath(id));
            var buildInfo = JsonUtil.ParseBuildInfo(id.JobId, data);
            var failureInfo = buildInfo.State == BuildState.Failed
                ? GetBuildFailureInfo(id)
                : null;
            return new BuildResult(buildInfo, failureInfo);
        }

        public async Task<BuildResult> GetBuildResultAsync(BuildId id)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetBuildPath(id));
            var buildInfo = JsonUtil.ParseBuildInfo(id.JobId, data);
            var failureInfo = buildInfo.State == BuildState.Failed
                ? await GetBuildFailureInfoAsync(id)
                : null;
            return new BuildResult(buildInfo, failureInfo);
        }

        public BuildFailureInfo GetBuildFailureInfo(BuildId id)
        {
            var data = GetJson(JenkinsUtil.GetBuildPath(id), tree: "actions[*]", depth: 4);
            return JsonUtil.ParseBuildFailureInfo(data);
        }

        public async Task<BuildFailureInfo> GetBuildFailureInfoAsync(BuildId id)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetBuildPath(id), tree: "actions[*]", depth: 4);
            return JsonUtil.ParseBuildFailureInfo(data);
        }

        public List<string> GetFailedTestCases(BuildId id)
        {
            var data = GetJson(JenkinsUtil.GetTestReportPath(id));
            return JsonUtil.ParseTestCaseListFailed(data);
        }

        public async Task<List<string>> GetFailedTestCasesAsync(BuildId id)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetTestReportPath(id));
            return JsonUtil.ParseTestCaseListFailed(data);
        }

        /// <summary>
        /// Get all of the queued items in Jenkins
        /// </summary>
        /// <returns></returns>
        public List<QueuedItemInfo> GetQueuedItemInfoList()
        {
            var data = GetJson("queue");
            return JsonUtil.ParseQueuedItemInfoList(data);
        }

        public async Task<List<QueuedItemInfo>> GetQueuedItemInfoListAsync()
        {
            var data = await GetJsonAsync("queue");
            return JsonUtil.ParseQueuedItemInfoList(data);
        }

        public List<ViewInfo> GetViews()
        {
            var data = GetJson("", tree: "views[*]");
            return JsonUtil.ParseViewInfoList(data);
        }

        public async Task<List<ViewInfo>> GetViewsAsync()
        {
            var data = await GetJsonAsync("", tree: "views[*]");
            return JsonUtil.ParseViewInfoList(data);
        }

        public List<ComputerInfo> GetComputerInfo()
        {
            var data = GetJson("computer", tree: "computers[*]");
            return JsonUtil.ParseComputerInfoList(data);
        }

        public async Task<List<ComputerInfo>> GetComputerInfoAsync()
        {
            var data = await GetJsonAsync("computer", tree: "computers[*]");
            return JsonUtil.ParseComputerInfoList(data);
        }

        public PullRequestInfo GetPullRequestInfo(BuildId id)
        {
            var data = GetJson(JenkinsUtil.GetBuildPath(id), tree: "actions");
            return JsonUtil.ParsePullRequestInfo((JArray)data["actions"]);
        }

        public async Task<PullRequestInfo> GetPullRequestInfoAsync(BuildId id)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetBuildPath(id), tree: "actions");
            return JsonUtil.ParsePullRequestInfo((JArray)data["actions"]);
        }

        public string GetConsoleText(BuildId id)
        {
            var uri = JenkinsUtil.GetUri(_baseUrl, JenkinsUtil.GetConsoleTextPath(id));
            var request = WebRequest.Create(uri);
            using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public async Task<string> GetConsoleTextAsync(BuildId id)
        {
            var uri = JenkinsUtil.GetUri(_baseUrl, JenkinsUtil.GetConsoleTextPath(id));
            var request = WebRequest.Create(uri);
            using (var reader = new StreamReader((await request.GetResponseAsync()).GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public JObject GetJson(string urlPath, bool pretty = false, string tree = null, int? depth = null)
        {
            var request = GetJsonRestRequest(urlPath, pretty, tree, depth);
            var response = _restClient.Execute(request);
            return JObject.Parse(response.Content);
        }

        public async Task<JObject> GetJsonAsync(string urlPath, bool pretty = false, string tree = null, int? depth = null)
        {
            var request = GetJsonRestRequest(urlPath, pretty, tree, depth);
            var response = await _restClient.ExecuteTaskAsync(request);
            return JObject.Parse(response.Content);
        }

        /// <summary>
        /// Build up the <see cref="RestRequest"/> object for the JSON query. 
        /// </summary>
        private RestRequest GetJsonRestRequest(string urlPath, bool pretty, string tree, int? depth)
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

            return request;
        }
    }
}
