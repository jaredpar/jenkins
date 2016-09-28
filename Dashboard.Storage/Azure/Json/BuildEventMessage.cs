using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dashboard.Jenkins;

namespace Dashboard.Azure.Json
{
    public sealed class BuildEventMessageJson
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
        public BoundBuildId BoundBuildId => new BoundBuildId(JenkinsHostName, BuildId);
    }
}
