using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Dashboard.Jenkins
{
    public sealed partial class JenkinsClient
    {
        private readonly Uri _host;
        private readonly IRestClient _restClient;
        private readonly string _authorizationHeaderValue;

        public IRestClient RestClient => _restClient;
        public Uri Host => _host;

        public JenkinsClient(Uri host)
        {
            _host = host;
            _restClient = new RestClient(host);
        }

        public JenkinsClient(Uri host, IRestClient restClient)
        {
            _host = host;
            _restClient = restClient;
        }

        public JenkinsClient(Uri host, string connectionString)
            : this(host)
        {
            var items = connectionString.Split(new[] { ':' }, count: 2);
            _authorizationHeaderValue = SharedUtil.CreateAuthorizationHeader(items[0], items[1]);
        }

        public JenkinsClient(Uri host, string username, string password) 
            : this(host, $"{username}:{password}")
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
            var data = GetJson(JenkinsUtil.GetJobPath(parent));
            return JsonUtil.ParseJobs(parent, (JArray)data["jobs"]);
        }

        public async Task<List<JobId>> GetJobIdsAsync(JobId parent = null)
        {
            parent = parent ?? JobId.Root;
            var data = await GetJsonAsync(JenkinsUtil.GetJobPath(parent));
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
            var data = GetJson(JenkinsUtil.GetJobPath(jobId));
            return JsonUtil.ParseBuilds(jobId, (JArray)data["builds"] ?? new JArray());
        }

        public async Task<List<BuildId>> GetBuildIdsAsync(JobId jobId)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetJobPath(jobId));
            return JsonUtil.ParseBuilds(jobId, (JArray)data["builds"] ?? new JArray());
        }

        public BuildInfo GetBuildInfo(BuildId id)
        {
            var data = GetJson(JenkinsUtil.GetBuildPath(id), tree: JsonUtil.BuildInfoTreeFilter);
            return JsonUtil.ParseBuildInfo(Host, id.JobId, data);
        }

        public async Task<BuildInfo> GetBuildInfoAsync(BuildId id)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetBuildPath(id), tree: JsonUtil.BuildInfoTreeFilter);
            return JsonUtil.ParseBuildInfo(Host, id.JobId, data);
        }

        /// <summary>
        /// Get all of the <see cref="BuildInfo"/> values for the specified <see cref="JobId"/>.
        /// </summary>
        public List<BuildInfo> GetBuildInfoList(JobId id)
        {
            var data = GetJson(JenkinsUtil.GetJobPath(id), tree: JsonUtil.BuildInfoListTreeFilter, depth: 2);
            return JsonUtil.ParseBuildInfoList(Host, id, data);
        }

        public async Task<List<BuildInfo>> GetBuildInfoListAsync(JobId id)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetJobPath(id), tree: JsonUtil.BuildInfoListTreeFilter, depth: 2);
            return JsonUtil.ParseBuildInfoList(Host, id, data);
        }

        public JobInfo GetJobInfo(JobId id)
        {
            var json = GetJson(JenkinsUtil.GetJobPath(id));
            var xml = GetXml(JenkinsUtil.GetJobPath(id));
            return GetJobInfoCore(id, json, xml);
        }

        public async Task<JobInfo> GetJobInfoAsync(JobId id)
        {
            var json = await GetJsonAsync(JenkinsUtil.GetJobPath(id));
            var xml = await GetXmlAsync(JenkinsUtil.GetJobPath(id));
            return GetJobInfoCore(id, json, xml);
        }

        private static JobInfo GetJobInfoCore(JobId id, JObject json, XElement xml)
        {
            var builds = JsonUtil.ParseBuilds(id, (json["builds"] as JArray) ?? new JArray()) ?? new List<BuildId>(capacity: 0);
            var jobs = JsonUtil.ParseJobs(id, (json["jobs"] as JArray) ?? new JArray()) ?? new List<JobId>(capacity: 0);
            var kind = XmlUtil.ParseJobKind(xml);
            return new JobInfo(id, kind, builds, jobs);
        }

        public string GetJobKind(JobId id)
        {
            var xml = GetXml(JenkinsUtil.GetJobPath(id));
            return XmlUtil.ParseJobKind(xml);
        }

        public async Task<string> GetJobKindAsync(JobId id)
        {
            var xml = await GetXmlAsync(JenkinsUtil.GetJobPath(id));
            return XmlUtil.ParseJobKind(xml);
        }

        public BuildResult GetBuildResult(BuildInfo buildInfo)
        {
            var failureInfo = buildInfo.State == BuildState.Failed
                ? GetBuildFailureInfo(buildInfo.BuildId)
                : null;
            return new BuildResult(buildInfo, failureInfo);
        }

        public async Task<BuildResult> GetBuildResultAsync(BuildInfo buildInfo)
        {
            var failureInfo = buildInfo.State == BuildState.Failed
                ? await GetBuildFailureInfoAsync(buildInfo.BuildId)
                : null;
            return new BuildResult(buildInfo, failureInfo);
        }

        public BuildFailureInfo GetBuildFailureInfo(BuildId id)
        {
            var data = GetJson(JenkinsUtil.GetBuildPath(id), depth: 4);
            return JsonUtil.ParseBuildFailureInfo(data);
        }

        public async Task<BuildFailureInfo> GetBuildFailureInfoAsync(BuildId id)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetBuildPath(id), depth: 4);
            return JsonUtil.ParseBuildFailureInfo(data);
        }

        public List<string> GetFailedTestCases(BuildId id)
        {
            var util = GetJsonReaderCore(JenkinsUtil.GetTestReportPath(id), JsonUtil.ParseTestCaseListFailed);
            return GetFailedTestCasesCore(util);
        }

        public async Task<List<string>> GetFailedTestCasesAsync(BuildId id)
        {
            var util = await GetJsonReaderCoreAsync(JenkinsUtil.GetTestReportPath(id), JsonUtil.ParseTestCaseListFailed);
            return GetFailedTestCasesCore(util);
        }

        private List<string> GetFailedTestCasesCore(JsonReaderUtil<List<string>> util)
        {
            if (util.Succeeded)
            {
                return util.Value;
            }

            // In the case there is no test file then simply return an empty list.  This is common in jobs that 
            // don't produce proper files.
            if (util.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<string>();
            }

            if (util.Exception != null)
            {
                throw util.Exception;
            }

            throw new Exception($"Unexpected error parsing test results: {util.Response.StatusCode}");
        }

        public QueuedItemInfo GetQueuedItemInfo(int number)
        {
            var data = GetJson(JenkinsUtil.GetQueuedItemPath(number));
            return JsonUtil.ParseQueuedItemInfo(data);
        }

        public async Task<QueuedItemInfo> GetQueuedItemInfoAsync(int number)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetQueuedItemPath(number));
            return JsonUtil.ParseQueuedItemInfo(data);
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
            var data = await GetJsonAsync(JenkinsUtil.GetBuildPath(id), tree: "actions[parameters[*]]");
            return JsonUtil.ParsePullRequestInfo((JArray)data["actions"]);
        }

        public TimeSpan? GetTimeInQueue(BuildId id)
        {
            var data = GetJson(JenkinsUtil.GetBuildPath(id), tree: "actions[*]");
            return JsonUtil.ParseTimeInQueue((JArray)data["actions"]);
        }

        public async Task<TimeSpan?> GetTimeInQueueAsync(BuildId id)
        {
            var data = await GetJsonAsync(JenkinsUtil.GetBuildPath(id), tree: "actions[*]");
            return JsonUtil.ParseTimeInQueue((JArray)data["actions"]);
        }

        public bool JobDelete(JobId jobId)
        {
            return DoAction(JenkinsUtil.GetJobDeletePath(jobId));
        }

        public async Task<bool> JobDeleteAsync(JobId jobId)
        {
            return await DoActionAsync(JenkinsUtil.GetJobDeletePath(jobId));
        }

        public bool JobEnable(JobId jobId)
        {
            return DoAction(JenkinsUtil.GetJobEnablePath(jobId));
        }

        public async Task<bool> JobEnableAsync(JobId jobId)
        {
            return await DoActionAsync(JenkinsUtil.GetJobEnablePath(jobId));
        }

        public bool JobDisable(JobId jobId)
        {
            return DoAction(JenkinsUtil.GetJobDisablePath(jobId));
        }

        public async Task<bool> JobDisableAsync(JobId jobId)
        {
            return await DoActionAsync(JenkinsUtil.GetJobDisablePath(jobId));
        }

        public string GetConsoleText(BuildId id)
        {
            var uri = JenkinsUtil.GetUri(_host, JenkinsUtil.GetConsoleTextPath(id));
            var request = WebRequest.Create(uri);
            using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public async Task<string> GetConsoleTextAsync(BuildId id)
        {
            var uri = JenkinsUtil.GetUri(_host, JenkinsUtil.GetConsoleTextPath(id));
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
            return ParseJsonCore(response);
        }

        public async Task<JObject> GetJsonAsync(string urlPath, bool pretty = false, string tree = null, int? depth = null)
        {
            var request = GetJsonRestRequest(urlPath, pretty, tree, depth);
            var response = await _restClient.ExecuteTaskAsync(request);
            return ParseJsonCore(response);
        }

        private JsonReaderUtil<T> GetJsonReaderCore<T>(string urlPath, Func<JsonReader, T> func, bool pretty = false, string tree = null, int? depth = null)
        {
            var util = new JsonReaderUtil<T>(func);
            var request = GetJsonRestRequest(urlPath, pretty, tree, depth);
            request.ResponseWriter = util.Run;
            util.Response = _restClient.Execute(request);
            return util;
        }

        private async Task<JsonReaderUtil<T>> GetJsonReaderCoreAsync<T>(string urlPath, Func<JsonReader, T> func, bool pretty = false, string tree = null, int? depth = null)
        {
            var util = new JsonReaderUtil<T>(func);
            var request = GetJsonRestRequest(urlPath, pretty, tree, depth);
            request.ResponseWriter = util.Run;
            util.Response = await _restClient.ExecuteTaskAsync(request);
            return util;
        }

        private static JObject ParseJsonCore(IRestResponse response)
        {
            try
            {
                return JObject.Parse(response.Content);
            }
            catch (Exception e)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"Unable to parse json");
                builder.AppendLine($"  Url: {response.ResponseUri}");
                builder.AppendLine($"  Status: {response.StatusDescription}");
                builder.AppendLine($"  Conent: {response.Content}");
                throw new Exception(builder.ToString(), e);
            }
        }

        public XElement GetXml(string urlPath)
        {
            var request = GetXmlRestRequest(urlPath);
            var response = _restClient.Execute(request);
            return ParseXmlCore(response);
        }

        public async Task<XElement> GetXmlAsync(string urlPath)
        {
            var request = GetXmlRestRequest(urlPath);
            var response = await _restClient.ExecuteTaskAsync(request);
            return ParseXmlCore(response);
        }

        private static XElement ParseXmlCore(IRestResponse response)
        {
            try
            {
                return XElement.Parse(response.Content);
            }
            catch (Exception e)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"Unable to parse xml");
                builder.AppendLine($"  Url: {response.ResponseUri}");
                builder.AppendLine($"  Status: {response.StatusDescription}");
                builder.AppendLine($"  Conent: {response.Content}");
                throw new Exception(builder.ToString(), e);
            }
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

        private RestRequest GetXmlRestRequest(string urlPath)
        {
            urlPath = urlPath.TrimEnd('/');
            var request = new RestRequest($"{urlPath}/api/xml", Method.GET);
            // TODO: xpath filter? 
            return request;
        }

        private bool DoAction(string path)
        {
            var request = GetActionRequest(path);
            var response = _restClient.Execute(request);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public async Task<bool> DoActionAsync(string path)
        {
            var request = GetActionRequest(path);
            var response = await _restClient.ExecuteTaskAsync(request);
            return response.StatusCode == HttpStatusCode.OK;
        }

        private RestRequest GetActionRequest(string path)
        {
            var request = new RestRequest(path);
            request.Method = Method.POST;

            if (!string.IsNullOrEmpty(_authorizationHeaderValue))
            {
                request.AddHeader("Authorization", _authorizationHeaderValue);
            }

            return request;
        }
    }
}
