using Dashboard.Jenkins;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    internal sealed class DemandBuildBuilder
    {
        private readonly Uri _jenkinsUrl;
        private readonly string _userName;
        private readonly string _token;
        private readonly Uri _repoUrl;
        private readonly string _commit;
        private readonly JenkinsClient _client;

        internal DemandBuildBuilder(Uri jenkinsUrl, string userName, string token, Uri repoUrl, string commit)
        {
            _jenkinsUrl = jenkinsUrl;
            _userName = userName;
            _token = token;
            _repoUrl = repoUrl;
            _commit = commit;
            _client = new JenkinsClient(jenkinsUrl, userName, token);
        }

        public async Task<DemandBuildEntity> CreateDemandBuild(JobId jobId)
        {
            var queuedItemNumber = await CreateJenkinsBuild(jobId);

            // DEMAND: hacky but good enough for this experiment.
            int? buildNumber = null;
            int count = 0;
            do
            {
                var info = await _client.GetQueuedItemInfoAsync(queuedItemNumber);
                buildNumber = info.BuildNumber;

                if (!buildNumber.HasValue)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                count++;
            } while (!buildNumber.HasValue && count < 5);

            return new DemandBuildEntity(_userName, _commit, queuedItemNumber, jobId.Name, buildNumber.Value);
        }

        private async Task<int> CreateJenkinsBuild(JobId jobId)
        {
            var path = $"{JenkinsUtil.GetJobIdPath(jobId)}/buildWithParameters";
            var request = new RestRequest(path, Method.POST);
            request.AddParameter("GitRepoUrl", _repoUrl.ToString());
            request.AddParameter("GitBranchOrCommit", _commit);
            SharedUtil.AddAuthorization(request, _userName, _token);

            var client = new RestClient(_jenkinsUrl);
            var response = await client.ExecutePostTaskAsync(request);

            // This comes back as the URI https://server/queue/item/number.  Need the 
            // number of the queue item.
            var location = response.Headers.First(x => x.Name == "Location");
            var queueItemUri = new Uri(Convert.ToString(location.Value));
            var last = queueItemUri.LocalPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
            return int.Parse(last);
        }
    }
}
