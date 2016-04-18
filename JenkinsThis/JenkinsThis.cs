using Dashboard.Jenkins;
using Dashboard.Json;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.JenkinsThis
{
    internal sealed class JenkinsThisUtil
    {
        // DEMAND: Use the real list
        internal static readonly JobId[] JobIds = new[]
        {
            // JobId.ParseName("roslyn_prtest_win_dbg_unit32"),
            JobId.ParseName("roslyn_prtest_lin_dbg_unit32")
        };

        private readonly string _userName;
        private readonly string _token;
        private readonly Uri _repoUri;
        private readonly string _branchOrCommit;

        internal JenkinsThisUtil(
            string userName,
            string token,
            Uri repoUri,
            string branchOrCommit)
        {
            _userName = userName;
            _token = token;
            _repoUri = repoUri;
            _branchOrCommit = branchOrCommit;
        }

        internal void Go()
        {
            var model = CreateModel();
            var obj = JsonConvert.SerializeObject(model);

            var request = new RestRequest("api/builds/demand", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("text/json", obj, ParameterType.RequestBody);

            var client = new RestClient(SharedConstants.DashboardDebugUri);
            var response = client.Execute(request);
            Console.WriteLine(response.Content);
        }

        private DemandBuildModel CreateModel()
        { 
            var model = new DemandBuildModel()
            {
                UserName = _userName,
                RepoUrl = _repoUri.ToString(),
                BranchOrCommit = _branchOrCommit
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
            request.AddParameter("GitBranchOrCommit", _branchOrCommit);
            SharedUtil.AddAuthorization(request, _userName, _token);

            var client = new RestClient(SharedConstants.DotnetJenkinsUri);
            var response = client.Execute(request);

            // This comes back as the URI https://server/queue/item/number.  Need the 
            // number of the queue item.
            var location = response.Headers.First(x => x.Name == "Location");
            var queueItemUri = new Uri(Convert.ToString(location.Value));
            var last = queueItemUri.LocalPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
            return int.Parse(last);
        }
    }
}
