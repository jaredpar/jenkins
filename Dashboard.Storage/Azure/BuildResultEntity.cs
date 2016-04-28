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
    /// Information about a build result.  The BuildId is unique to this entity irrespective of 
    /// how it is stored.
    /// </summary>
    public sealed class BuildResultEntity : TableEntity
    {
        public string JobName { get; set; }
        public int BuildNumber { get; set; }
        public string BuildResultKindRaw { get; set; }
        public DateTime BuildDateTime { get; set; }
        public string MachineName { get; set; }

        public DateTimeOffset BuildDateTimeOffset => new DateTimeOffset(BuildDateTime);
        public JobId JobId => BuildId.JobId;
        public BuildId BuildId => new BuildId(BuildNumber, JobId);
        public BuildResultKind BuildResultKind => (BuildResultKind)Enum.Parse(typeof(BuildResultKind), BuildResultKindRaw);

        public BuildResultEntity()
        {

        }

        public BuildResultEntity(
            BuildId buildId,
            DateTimeOffset buildDateTime,
            string machineName,
            BuildResultKind kind)
        {
            JobName = buildId.JobId.Name;
            BuildNumber = buildId.Id;
            BuildResultKindRaw = kind.ToString();
            BuildDateTime = buildDateTime.UtcDateTime;
            MachineName = machineName;

            Debug.Assert(BuildDateTime.Kind == DateTimeKind.Utc);
        }

        public BuildResultEntity(BuildResultEntity other) : this(
            buildId: other.BuildId,
            buildDateTime: other.BuildDateTimeOffset,
            machineName: other.MachineName,
            kind: other.BuildResultKind)
        {

        }

        public EntityKey GetExactEntityKey()
        {
            return GetExactEntityKey(BuildId);
        }

        public static EntityKey GetExactEntityKey(BuildId buildId)
        {
            var partitionKey = AzureUtil.NormalizeKey(buildId.JobId.Name, '_');
            var rowKey = buildId.Id.ToString("0000000000");
            return new EntityKey(partitionKey, rowKey);
        }

        public EntityKey GetDateEntityKey()
        {
            return GetDateEntityKey(BuildDateTimeOffset, BuildId);
        }

        public static EntityKey GetDateEntityKey(DateTimeOffset buildDate, BuildId buildId)
        {
            return new EntityKey(
                new DateKey(buildDate).Key,
                new BuildKey(buildId).Key);
        }
    }
}
