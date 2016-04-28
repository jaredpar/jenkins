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
    public sealed class BuildResultDateEntity : BuildResultEntityBase
    {
        public const string TableName = AzureConstants.TableNames.BuildResultDate;

        public DateKey DateKey => DateKey.Parse(PartitionKey);
        public BuildKey BuildKey => BuildKey.Parse(RowKey);

        public BuildResultDateEntity()
        {

        }

        public BuildResultDateEntity(DateTimeOffset buildDateTime, BuildId buildId, string machineName, BuildResultKind kind)
            : base(
                  buildId: buildId,
                  buildDateTime: buildDateTime,
                  machineName: machineName,
                  kind: kind)
        {
            var key = GetEntityKey(buildDateTime, buildId);
            PartitionKey = key.PartitionKey;
            RowKey = key.RowKey;
        }

        public BuildResultDateEntity(BuildResultEntityBase other) : base(other)
        {
            var key = GetEntityKey(BuildDateTimeOffset, other.BuildId);
            PartitionKey = key.PartitionKey;
            RowKey = key.RowKey;
        }

        public static EntityKey GetEntityKey(DateTimeOffset buildDate, BuildId buildId)
        {
            return new EntityKey(
                new DateKey(buildDate).Key,
                new BuildKey(buildId).Key);
        }
    }
}
