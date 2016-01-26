using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public sealed class RoslynClient
    {
        public static readonly Uri RoslynJenkinsHost = new Uri("https://dotnet-ci.cloudapp.net");

        private readonly JenkinsClient _client;

        public JenkinsClient Client => _client;

        public RoslynClient()
        {
            _client = new JenkinsClient(RoslynJenkinsHost);
        }

        public RoslynClient(string userName, string password)
        {
            _client = new JenkinsClient(RoslynJenkinsHost, userName, password);
        }

        public List<string> GetJobNames()
        {
            return _client.GetJobNamesInView("roslyn");
        }

        public List<string> GetPullRequestJobNames()
        {
            // TODO: Hacky, should find an API way to git this information
            return GetJobNames()
                .Where(x => x.Contains("_pr"))
                .ToList();
        }

        public TimeSpan? GetTimeInQueue(JobId jobId)
        {
            try
            {
                var json = _client.GetJson(JenkinsUtil.GetJobPath(jobId), pretty: true, tree: "actions[*]");
                var actions = (JArray)json["actions"];
                foreach (var cur in actions)
                {
                    var value = cur.Value<int?>("queuingDurationMillis");
                    if (value.HasValue)
                    {
                        return TimeSpan.FromMilliseconds(value.Value);
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        public List<string> GetCommitJobNames()
        {
            // TODO: Hacky, should find an API way to git this information
            return GetJobNames()
                .Where(x => !x.Contains("_pr"))
                .ToList();
        }
    }
}
