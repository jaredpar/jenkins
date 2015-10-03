using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins.Jenkins
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
            return _client
                .GetJobNames()
                .Where(x => x.Contains("roslyn"))
                .ToList();
        }
    }
}
