using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Dashboard.Jenkins
{
    internal static class JsonUtil
    {
        internal const string BuildInfoTreeFilter = "result,id,duration,timestamp,builtOn";
        internal const string BuildInfoListTreeFilter = "builds[result,id,duration,timestamp,builtOn]";

        internal static List<BuildId> ParseBuilds(JobId id, JArray builds)
        {
            Debug.Assert(builds != null);
            var list = new List<BuildId>();
            foreach (var cur in builds)
            {
                var build = cur.ToObject<Json.Build>();
                list.Add(new BuildId(build.Number, id));
            }

            return list;
        }

        internal static List<JobId> ParseJobs(JobId parent, JArray jobs)
        {
            if (jobs == null)
            {
                return new List<JobId>(capacity: 0);
            }

            var list = new List<JobId>();
            foreach (var cur in jobs)
            {
                var name = cur.Value<string>("name");
                var id = new JobId(name, parent);
                list.Add(id);
            }

            return list;
        }

        internal static BuildInfo ParseBuildInfo(string hostName, JobId jobId, JObject build)
        {
            var id = build.Value<int>("id");
            var duration = TimeSpan.FromMilliseconds(build.Value<int>("duration"));
            var state = ParseBuildInfoState(build);
            var date = JenkinsUtil.ConvertTimestampToDateTimeOffset(build.Value<long>("timestamp"));
            var buildId = new BoundBuildId(hostName, id, jobId);
            var machineName = build.Value<string>("builtOn");
            return new BuildInfo(buildId, state, date, duration, machineName);
        }

        internal static List<BuildInfo> ParseBuildInfoList(string hostName, JobId jobId, JObject data)
        {
            var list = new List<BuildInfo>();
            var builds = (JArray)data["builds"];
            foreach (JObject build in builds)
            {
                list.Add(ParseBuildInfo(hostName, jobId, build));
            }

            return list;
        }

        internal static BuildFailureInfo ParseBuildFailureInfo(JObject data)
        {
            BuildFailureInfo info;
            if (!BuildFailureUtil.TryGetBuildFailureInfo(data, out info))
            {
                info = BuildFailureInfo.Unknown;
            }

            return info;
        }

        internal static List<QueuedItemInfo> ParseQueuedItemInfoList(JObject data)
        {
            var items = (JArray)data["items"];
            var list = new List<QueuedItemInfo>();
            foreach (JObject item in items)
            {
                list.Add(ParseQueuedItemInfo(item));
            }

            return list;
        }

        internal static QueuedItemInfo ParseQueuedItemInfo(JObject data)
        {
            PullRequestInfo prInfo;
            TryParsePullRequestInfo((JArray)data["actions"], out prInfo);
            var id = data.Value<int>("id");
            var jobUrlStr = data["task"].Value<string>("url");
            var jobUrl = new Uri(jobUrlStr);
            var jobId = JenkinsUtil.ConvertPathToJobId(jobUrl.LocalPath);

            int? buildNumber = null;
            var executable = data["executable"] as JObject;
            if (executable != null)
            {
                buildNumber = executable.Value<int?>("number");
            }

            return new QueuedItemInfo(id, jobId, prInfo, buildNumber);
        }

        internal static List<ViewInfo> ParseViewInfoList(JObject data)
        {
            var list = new List<ViewInfo>();
            var items = (JArray)data["views"];
            foreach (JObject viewData in items)
            {
                list.Add(ParseViewInfo(viewData));
            }

            return list;
        }

        internal static ViewInfo ParseViewInfo(JObject data)
        {
            var name = data.Value<string>("name");
            var description = data.Value<string>("description");
            var url = new Uri(data.Value<string>("url"));
            return new ViewInfo(name, description, url);
        }

        internal static List<ComputerInfo> ParseComputerInfoList(JObject data)
        {
            var list = new List<ComputerInfo>();
            var items = (JArray)data["computer"];
            foreach (JObject item in items)
            {
                list.Add(ParseComputerInfo(item));
            }

            return list;
        }

        internal static ComputerInfo ParseComputerInfo(JObject data)
        {
            var name = data.Value<string>("displayName");
            var os = data["monitorData"].Value<string>("hudson.node_monitors.ArchitectureMonitor");
            return new ComputerInfo(name, os);
        }

        /// <summary>
        /// Get all of the failed test case names from the testReport
        /// </summary>
        /// <remarks>
        /// It's possible for the testReport to be quite large.  In some cases it can be hundreds of megabytes in length.  Hence
        /// we use a reader here vs. a full DOM to save on the allocations. 
        /// </remarks>
        internal static List<string> ParseTestCaseListFailed(JsonReader reader)
        {
            return BuildFailureUtil.GetTestCaseFailureList(reader);
        }

        /// <summary>
        /// Parse out the time a given build spent in the queue.  This is available in the JSON
        /// for a build under the "actions" section.
        /// </summary>
        internal static TimeSpan? ParseTimeInQueue(JArray actions)
        {
            foreach (var cur in actions)
            {
                var value = cur.Value<int?>("queuingDurationMillis");
                if (value.HasValue)
                {
                    return TimeSpan.FromMilliseconds(value.Value);
                }
            }

            return null;
        }

        private static BuildState ParseBuildInfoState(JObject build)
        {
            var result = build.Property("result");
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

        /// <summary>
        /// Is this a child build job.  If so return the ID of the parent job and base url
        /// </summary>
        internal static bool IsChildJob(JArray actions, out string baseUrl, out int parentBuildId)
        {
            baseUrl = null;
            parentBuildId = 0;

            var obj = actions.FirstOrDefault(x => x["causes"] != null);
            if (obj == null)
            {
                return false;
            }

            var array = (JArray)obj["causes"];
            if (array.Count == 0)
            {
                return false;
            }

            var data = array[0];
            baseUrl = data.Value<string>("upstreamUrl");
            parentBuildId = data.Value<int>("upstreamBuild");
            return baseUrl != null && parentBuildId != 0;
        }

        internal static PullRequestInfo ParsePullRequestInfo(JArray actions)
        {
            PullRequestInfo info;
            if (!TryParsePullRequestInfo(actions, out info))
            {
                throw new Exception("Could not read pull request data");
            }

            return info;
        }

        internal static bool TryParsePullRequestInfo(JArray actions, out PullRequestInfo info)
        {
            var container = actions.FirstOrDefault(x => x["parameters"] != null);
            if (container == null)
            {
                info = null;
                return false;
            }

            string sha1 = null;
            string pullLink = null;
            int? pullId = null;
            string pullAuthor = null;
            string pullAuthorEmail = null;
            string commitAuthorEmail = null;
            var parameters = (JArray)container["parameters"];
            foreach (var pair in parameters)
            {
                switch (pair.Value<string>("name"))
                {
                    case "ghprbActualCommit":
                        sha1 = pair.Value<string>("value") ?? sha1;
                        break;
                    case "ghprbPullId":
                        pullId = pair.Value<int>("value");
                        break;
                    case "ghprbPullAuthorLogin":
                        pullAuthor = pair.Value<string>("value") ?? pullAuthor;
                        break;
                    case "ghprbPullAuthorEmail":
                        pullAuthorEmail = pair.Value<string>("value") ?? pullAuthorEmail;
                        break;
                    case "ghprbActualCommitAuthorEmail":
                        commitAuthorEmail = pair.Value<string>("value") ?? commitAuthorEmail;
                        break;
                    case "ghprbPullLink":
                        pullLink = pair.Value<string>("value") ?? pullLink;
                        break;
                    default:
                        break;
                }
            }

            // It's possible for the pull email to be blank if the Github settings for the user 
            // account hides their public email address.  In that case fall back to the commit 
            // author.  It's generally the same value and serves as a nice backup identifier.
            if (string.IsNullOrEmpty(pullAuthorEmail))
            {
                pullAuthorEmail = commitAuthorEmail;
            }

            if (sha1 == null || pullLink == null || pullId == null || pullAuthorEmail == null)
            {
                info = null;
                return false;
            }

            info = new PullRequestInfo(
                author: pullAuthor,
                authorEmail: pullAuthorEmail,
                id: pullId.Value,
                pullUrl: pullLink,
                sha1: sha1);
            return true;
        }

        private static string GetSha1Core(JObject data)
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

    }
}
