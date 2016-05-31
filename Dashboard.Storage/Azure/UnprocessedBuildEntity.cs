using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    /// <summary>
    /// Represents builds which have not yet been fully processed.
    /// </summary>
    public class UnprocessedBuildEntity : TableEntity
    {
        public string JobName { get; set; }
        public int BuildNumber { get; set; }
        public DateTime LastUpdate { get; set; }
        public string StatusText { get; set; }
        public string HostName { get; set; }
        public string UriScheme { get; set; }

        public JobId JobId => JobId.ParseName(JobName);
        public BuildId BuildId => new BuildId(BuildNumber, JobId);
        public BoundBuildId BoundBuildId => new BoundBuildId(HostName ?? "dotnet-ci.cloudapp.net", BuildId, UriScheme ?? Uri.UriSchemeHttps);

        public UnprocessedBuildEntity()
        {

        }

        public UnprocessedBuildEntity(BoundBuildId boundBuildId)
        {
            var buildId = boundBuildId.BuildId;
            var entityKey = GetEntityKey(buildId);
            PartitionKey = entityKey.PartitionKey;
            RowKey = entityKey.RowKey;
            JobName = buildId.JobName;
            BuildNumber = buildId.Number;
            LastUpdate = DateTime.UtcNow;
            HostName = boundBuildId.HostName;
            UriScheme = boundBuildId.UriScheme;
        }

        public static EntityKey GetEntityKey(BuildId buildId)
        {
            var partitionKey = AzureUtil.NormalizeKey(buildId.JobId.Name, '_');
            var rowKey = buildId.Number.ToString();
            return new EntityKey(partitionKey, rowKey);
        }
    }
}
