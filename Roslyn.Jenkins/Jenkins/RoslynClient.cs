using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public sealed class RoslynClient
    {
        private readonly JenkinsClient _client;

        public JenkinsClient Client => _client;

        public RoslynClient()
        {
            _client = new JenkinsClient();
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

        public List<string> GetCommitJobNames()
        {
            // TODO: Hacky, should find an API way to git this information
            return GetJobNames()
                .Where(x => !x.Contains("_pr"))
                .ToList();
        }
    }
}
