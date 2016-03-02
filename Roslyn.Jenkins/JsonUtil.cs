using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    internal static class JsonUtil
    {
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

            return null;
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
            string pullAuthorEmail = null;
            string commitAuthorEmail = null;
            var parameters = (JArray)container["parameters"];
            foreach (var pair in parameters)
            {
                switch (pair.Value<string>("name"))
                {
                    case "ghprbActualCommit":
                        sha1 = pair.Value<string>("value");
                        break;
                    case "ghprbPullId":
                        pullId = pair.Value<int>("value");
                        break;
                    case "ghprbPullAuthorEmail":
                        pullAuthorEmail = pair.Value<string>("value");
                        break;
                    case "ghprbActualCommitAuthorEmail":
                        commitAuthorEmail = pair.Value<string>("value");
                        break;
                    case "ghprbPullLink":
                        pullLink = pair.Value<string>("value");
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
                authorEmail: pullAuthorEmail,
                id: pullId.Value,
                pullUrl: pullLink,
                sha1: sha1);
            return true;
        }
    }
}
