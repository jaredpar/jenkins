using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.StorageBuilder
{
    internal sealed class BuildEventMessageJson
    {
        public string JobName { get; set; }
        public string Url { get; set; }
        public string Phase { get; set; }
        public string Status { get; set; }
        public int Number { get; set; }
        public int QueueId { get; set; }

        public BuildId BuildId => new BuildId(Number, JobId);
        public JobId JobId => JenkinsUtil.ConvertPathToJobId(JobName);
        public string JenkinsHostName => (new Uri(Url)).Host;
    }

    internal sealed class BuildIdJson
    {
        public string JenkinsHostName { get; set; }
        public string JobName { get; set; }
        public int BuildNumber { get; set; }

        public Uri JenkinsUrl => new Uri($"https://{JenkinsHostName}");
    }
}
