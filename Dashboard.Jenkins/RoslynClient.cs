using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Jenkins
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

        public RoslynClient(string connectionString)
        {
            _client = new JenkinsClient(RoslynJenkinsHost, connectionString);
        }

        public RoslynClient(string userName, string password)
        {
            _client = new JenkinsClient(RoslynJenkinsHost, userName, password);
        }

        public List<JobId> GetJobIds()
        {
            return _client.GetJobIdsInView("Roslyn");
        }

        public List<string> GetJobNames()
        {
            return _client.GetJobIdsInView("Roslyn").Select(x => x.Name).ToList();
        }

        public static bool IsPullRequestJobName(string jobName)
        {
            // TODO: This is super hacky.  But for now it's a correct hueristic and is workable.
            return jobName.Contains("_pr");
        }

        public static bool IsCommitJobName(string jobName)
        {
            return !IsPullRequestJobName(jobName);
        }

        public TimeSpan? GetTimeInQueue(BuildId jobId)
        {
            try
            {
                var json = _client.GetJson(JenkinsUtil.GetBuildPath(jobId), pretty: true, tree: "actions[*]");
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
    }
}
