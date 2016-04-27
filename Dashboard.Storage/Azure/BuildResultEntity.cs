using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    /// <summary>
    /// Table entity designed for querying build results by the date on which they occured.  Uses a 
    /// <see cref="DatePartitionKey"/> to create range partitions for date based query.
    /// </summary>
    public sealed class BuildResultEntity : TableEntity
    {
        public const string TableName = AzureConstants.TableNames.BuildResult;

        public string BuildResultKindRaw { get; set; }
        public DateTime BuildDateTime { get; set; }
        public string JobName { get; set; }
        public int BuildNumber { get; set; }
        public string MachineName { get; set; }

        public DateKey DateKey => DateKey.Parse(PartitionKey);
        public BuildKey BuildKey => BuildKey.Parse(RowKey);
        public BuildId BuildId => BuildKey.BuildId;
        public JobId JobId => BuildId.JobId;
        public BuildResultKind BuildResultKind => (BuildResultKind)Enum.Parse(typeof(BuildResultKind), BuildResultKindRaw);

        public BuildResultEntity()
        {

        }

        public BuildResultEntity(BuildProcessedEntity entity) : this(entity.BuildDate, entity.BuildId, entity.MachineName, entity.Kind)
        {

        }

        public BuildResultEntity(DateTimeOffset buildDate, BuildId buildId, string machineName, BuildResultKind buildResultKind)
        {
            var key = GetEntityKey(buildDate, buildId);
            PartitionKey = key.PartitionKey;
            RowKey = key.RowKey;
            BuildResultKindRaw = buildResultKind.ToString();
            BuildDateTime = buildDate.ToUniversalTime().UtcDateTime;
            JobName = buildId.JobId.Name;
            BuildNumber = buildId.Id;
            MachineName = machineName;
            Debug.Assert(BuildDateTime.Kind == DateTimeKind.Utc);
        }

        public static EntityKey GetEntityKey(DateTimeOffset buildDate, BuildId buildId)
        {
            return new EntityKey(
                new DateKey(buildDate).Key,
                new BuildKey(buildId).Key);
        }
    }
}
