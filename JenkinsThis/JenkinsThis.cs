using Dashboard.Jenkins;
using Dashboard.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.JenkinsThis
{
    internal sealed class JenkinsThis
    {
        // DEMAND: Use the real list
        internal static readonly JobId[] JobIds = new[]
        {
            JobId.ParseName("roslyn_prtest_win_dbg_unit32"),
            JobId.ParseName("roslyn_prtest_lin_dbg_unit32")
        };

        private readonly string _userName;
        private readonly string _token;
        private readonly Uri _repoUri;
        private readonly string _branchName;
        private readonly string _sha1;

        internal JenkinsThis(
            string userName,
            string token,
            Uri repoUri,
            string branchName,
            string sha1)
        {
            _userName = userName;
            _token = token;
            _repoUri = repoUri;
            _branchName = branchName;
            _sha1 = sha1;
        }

        internal void Go()
        {
            var model = CreateModel();

        }

        private DemandBuildModel CreateModel()
        { 
            var model = new DemandBuildModel()
            {
                UserName = _userName,
                RepoUrl = _repoUri.ToString(),
                Sha1 = _sha1
            };

            foreach (var jobId in JobIds)
            {
                var number = PostToJenkins(jobId);
                model.QueuedItems.Add(new DemandBuildItem()
                {
                    JobName = jobId.Name,
                    QueueItemNumber = number
                });
            }

            return model;
        }

        private int PostToJenkins(JobId jobId)
        {
            var path = $"{JenkinsUtil.GetJobIdPath(jobId)}/buildWithParameters";
            var request = new RestRequest(path, Method.POST);
            request.AddParameter("GitRepoUrl", _repoUri.ToString());
            request.AddParameter("GitBranchOrCommit", _sha1);
            SharedUtil.AddAuthorization(request, _userName, _token);

            var client = new RestClient(SharedConstants.DotnetJenkinsUri);
            var response = client.Execute(request);
            var location = response.Headers.First(x => x.Name == "Location");
            return Convert.ToInt32(location.Value);
        }
    }
}
