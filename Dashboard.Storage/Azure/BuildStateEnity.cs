using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Dashboard.Azure
{
    /// <summary>
    /// Represents the state of data for a build that is tracked.  The key is a <see cref="DateTimeKey"/> based
    /// on date to make for easy querying.
    /// </summary>
    public sealed class BuildStateEnity : TableEntity
    {
        public static DateTimeKeyFlags Flags => DateTimeKeyFlags.Date;

        public string HostName { get; set; }
        public int BuildNumber { get; set; }
        public string JobName { get; set; }

        /// <summary>
        /// Has the build itself finished.
        /// </summary>
        public bool IsBuildFinished { get; set; }

        /// <summary>
        /// Set when the data is complete and needs no more processing.
        /// </summary>
        public bool IsDataComplete { get; set; }

        /// <summary>
        /// In the case there was an error processing the build this holds the error text.
        /// </summary>
        public string Error { get; set; }

        public JobId JobId => JobId.ParseName(JobName);
        public BuildId BuildId => new BuildId(BuildNumber, JobId);
        public BoundBuildId BoundBuildID => new BoundBuildId(HostName, BuildId);

        public BuildStateEnity(DateTimeKey key, BoundBuildId buildId, bool isBuildFinished)
        {
            PartitionKey = key.Key;
            RowKey = GetRowKey(buildId);
            HostName = buildId.HostName;
            BuildNumber = buildId.Number;
            JobName = buildId.JobName;
            IsBuildFinished = isBuildFinished;
        }

        public BuildStateEnity()
        {

        }

        public static EntityKey GetEntityKey(DateTimeKey key, BoundBuildId buildId) => new EntityKey(key.Key, GetRowKey(buildId));
        public static EntityKey GetEntityKey(DateTimeOffset dateTime, BoundBuildId buildId) => new EntityKey(GetPartitionKey(dateTime), GetRowKey(buildId));
        public static string GetPartitionKey(DateTimeKey key) => key.Key;
        public static string GetPartitionKey(DateTimeOffset dateTime) => DateTimeKey.GetKey(dateTime, Flags);
        public static string GetRowKey(BoundBuildId buildId) => BuildKey.GetKey(buildId.BuildId);
    }
}
