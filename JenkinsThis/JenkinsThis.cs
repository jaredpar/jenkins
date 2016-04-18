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

        private DemandRunRequestModel CreateModel()
        {
            var model = new DemandRunRequestModel()
            {
                UserName = _userName,
                Token = _token,
                RepoUrl = _repoUri.ToString(),
                BranchOrCommit = _branchOrCommit
            };

            foreach (var jobId in JobIds)
            {
                model.JobNames.Add(jobId.Name);
            }

            return model;
        }
    }
}
