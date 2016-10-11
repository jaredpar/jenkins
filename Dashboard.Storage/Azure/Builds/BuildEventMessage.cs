using System;
using Dashboard.Jenkins;

namespace Dashboard.Azure.Builds
{
    /// <summary>
    /// JSON serializable type that represent build event information from Jenkins.
    /// </summary>
    public sealed class BuildEventMessageJson
    {
        public string JobName { get; set; }
        public string Url { get; set; }
        public string Phase { get; set; }
        public string Status { get; set; }
        public int Number { get; set; }
        public int QueueId { get; set; }

        public Uri Host => BoundBuildId.NormalizeHostUri(new Uri(Url));
        public JobId JobId => JenkinsUtil.ConvertPathToJobId(JobName);
        public BuildId BuildId => new BuildId(Number, JobId);
        public BoundBuildId BoundBuildId => new BoundBuildId(Host, BuildId);
    }
}
