using Dashboard.Jenkins;
using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Dashboard.Azure.Builds
{
    /// <summary>
    /// Represents the state of data for a build that is tracked.  The key is a <see cref="DateTimeKey"/> based
    /// on date to make for easy querying.
    /// </summary>
    public sealed class BuildStateEntity : TableEntity
    {
        public static DateTimeKeyFlags Flags => DateTimeKeyFlags.Date;

        public string HostName { get; set; }
        public string HostRaw { get; set; }
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

        public DateTimeKey BuildStateKey => DateTimeKey.ParseDateTimeKey(PartitionKey, Flags);
        public JobId JobId => JobId.ParseName(JobName);
        public Uri Host => HostRaw != null ? new Uri(HostRaw) : new Uri($"http://{HostName}");
        public BuildId BuildId => new BuildId(BuildNumber, JobId);
        public BoundBuildId BoundBuildId => new BoundBuildId(Host, BuildId);

        public BuildStateEntity(DateTimeKey key, BoundBuildId buildId, bool isBuildFinished)
        {
            PartitionKey = key.Key;
            RowKey = GetRowKey(buildId);
            HostRaw = buildId.Host.ToString();
            BuildNumber = buildId.Number;
            JobName = buildId.JobName;
            IsBuildFinished = isBuildFinished;
        }

        public BuildStateEntity()
        {

        }

        public static EntityKey GetEntityKey(DateTimeKey key, BoundBuildId buildId) => new EntityKey(key.Key, GetRowKey(buildId));
        public static EntityKey GetEntityKey(DateTimeOffset dateTime, BoundBuildId buildId) => new EntityKey(GetPartitionKey(dateTime), GetRowKey(buildId));
        public static string GetPartitionKey(DateTimeKey key) => key.Key;
        public static string GetPartitionKey(DateTimeOffset dateTime) => DateTimeKey.GetKey(dateTime, Flags);
        public static string GetRowKey(BoundBuildId buildId) => BuildKey.GetKey(buildId.BuildId);
    }
}
