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
        private readonly RestClient _restClient = new RestClient(JenkinsUtil.JenkinsHost.ToString());

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

        public List<JobId> GetJobIds()
        {
            var all = GetJobNames().ToArray();
            return GetJobIds(all);
        }

        public List<JobId> GetJobIds(string jobName)
        {
            var data = GetJson($"job/{jobName}/");
            var all = (JArray)data["builds"];
            var list = new List<JobId>();

            foreach (var cur in all)
            {
                var build = cur.ToObject<Json.Build>();
                list.Add(new JobId(build.Number, jobName));
            }

            return list;
        }

        public List<JobId> GetJobIds(params string[] jobNames)
        {
            var list = new List<JobId>();
            foreach (var name in jobNames)
            {
                list.AddRange(GetJobIds(name));
            }

            return list;
        }

        public JobInfo GetJobInfo(JobId id)
        {
            var data = GetJson(id);
            var sha1 = GetSha1Core(data);
            var state = GetJobStateCore(data);
            var date = GetJobDateCore(data);
            var duration = TimeSpan.FromMilliseconds(data.Value<int>("duration"));
            return new JobInfo(id, state, sha1, date, duration);
        }

        public JobResult GetJobResult(JobId id)
        {
            var data = GetJson(id);
            var state = GetJobStateCore(data);
            var date = GetJobDateCore(data);
            var jobInfo = new JobInfo(
                id,
                state,
                GetSha1Core(data),
                date,
                TimeSpan.FromMilliseconds(data.Value<int>("duration")));

            if (state == JobState.Failed)
            {
                var failureInfo = GetJobFailureInfo(id, data);
                return new JobResult(jobInfo, failureInfo);
            }

            return new JobResult(jobInfo);
        }

        public JobState GetJobState(JobId id)
        {
            var data = GetJson(id);
            return GetJobStateCore(data);
        }

        private DateTime GetJobDateCore(JObject data)
        {
            var seconds = data.Value<long>("timestamp");
            var epoch = new DateTime(year: 1970, month: 1, day: 1);
            return epoch.AddMilliseconds(seconds);
        }

        private JobState GetJobStateCore(JObject data)
        {
            var result = data.Property("result");
            if (result == null)
            {
                throw new Exception("Could not find the result property");
            }

            JobState? state = null;
            switch (result.Value.Value<string>())
            {
                case "SUCCESS":
                    state = JobState.Succeeded;
                    break;
                case "FAILURE":
                    state = JobState.Failed;
                    break;
                case "ABORTED":
                    state = JobState.Aborted;
                    break;
                case null:
                    state = JobState.Running;
                    break;
            }

            if (state == null)
            {
                throw new Exception("Unable to determine the success / failure of the job");
            }

            return state.Value;
        }

        public PullRequestInfo GetPullRequestInfo(JobId id)
        {
            var data = GetJson(id);
            return GetPullRequestInfoCore(id, data);
        }

        private PullRequestInfo GetPullRequestInfoCore(JobId id, JObject data)
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

        public string GetConsoleText(JobId id)
        {
            var uri = JenkinsUtil.GetConsoleTextUri(id);
            var request = WebRequest.Create(uri);
            using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        private JObject GetJson(JobId jobId)
        {
            var path = JenkinsUtil.GetJobPath(jobId);
            return GetJson(path);
        }

        private JObject GetJson(string urlPath)
        {
            urlPath = urlPath.TrimEnd('/');
            var request = new RestRequest($"{urlPath}/api/json", Method.GET);
            request.AddParameter("pretty", "true");
            var content = _restClient.Execute(request).Content;
            return JObject.Parse(content);
        }

        /// <summary>
        /// Attempt to determine the failure reason for the given Job.  This should  only be called on 
        /// jobs that are known to have failed.
        /// </summary>
        private JobFailureInfo GetJobFailureInfo(JobId jobId, JObject data)
        {
            // First look for the test failure information.  
            List<string> failedTestList;
            if (TryGetTestFailureReason(jobId, data, out failedTestList))
            {
                Debug.Assert(failedTestList.Count > 0);
                return new JobFailureInfo(JobFailureReason.TestCase, failedTestList);
            }

            // Now look at the console text.
            var consoleText = GetConsoleText(jobId);
            JobFailureInfo failureInfo;
            if (ConsoleTextUtil.TryGetFailureInfo(consoleText, out failureInfo))
            {
                return failureInfo;
            }

            return JobFailureInfo.Unknown;
        }

        // TODO: This should be in JenkinsUtil
        private bool TryGetTestFailureReason(JobId jobId, JObject data, out List<string> failedTestList)
        {
            var actions = (JArray)data["actions"];
            foreach (var cur in actions)
            {
                var failCount = cur.Value<int?>("failCount");
                if (failCount != null && failCount.Value != 0)
                {
                    var testReportUrl = cur.Value<string>("urlName");
                    var path = $"{JenkinsUtil.GetJobPath(jobId)}{testReportUrl}/";
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
