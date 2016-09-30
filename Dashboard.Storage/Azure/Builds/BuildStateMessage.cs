using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dashboard.Jenkins;

namespace Dashboard.Azure.Builds
{
    /// <summary>
    /// JSON serializable type that represents a build in progress or for which data collection hasn't
    /// completed.
    /// </summary>
    public sealed class BuildStateMessage
    {
        public string BuildStateKeyRaw { get; set; }
        public string HostName { get; set; }
        public string JobName { get; set; }
        public int BuildNumber { get; set; }

        public BuildStateMessage()
        {

        }

        public BuildStateMessage(DateTimeKey buildStateKey, BoundBuildId buildId)
        {
            BuildStateKeyRaw = buildStateKey.Key;
            HostName = buildId.HostName;
            JobName = buildId.JobName;
            BuildNumber = buildId.Number;
        }

        public DateTimeKey BuildStateKey => DateTimeKey.ParseDateTimeKey(BuildStateKeyRaw, BuildStateEntity.Flags);
        public JobId JobId => JobId.ParseName(JobName);
        public BuildId BuildId => new BuildId(BuildNumber, JobId);
        public BoundBuildId BoundBuildId => new BoundBuildId(HostName, BuildId);
    }
}
