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
    /// Table entity designed for querying build results by the build Id.
    /// </summary>
    public sealed class BuildResultExactEntity : BuildResultEntityBase
    {
        public const string TableName = AzureConstants.TableNames.BuildResultExact;

        public BuildResultExactEntity()
        {

        }

        public BuildResultExactEntity(DateTimeOffset buildDateTime, BuildId buildId, string machineName, BuildResultKind kind)
            : base(
                  buildId: buildId,
                  buildDateTime: buildDateTime,
                  machineName: machineName,
                  kind: kind)
        {
            var key = GetEntityKey(buildId);
            PartitionKey = key.PartitionKey;
            RowKey = key.RowKey;
        }

        public BuildResultExactEntity(BuildResultEntityBase other) : base(other)
        {
            var key = GetEntityKey(other.BuildId);
            PartitionKey = key.PartitionKey;
            RowKey = key.RowKey;
        }

        public static string GetPartitionKey(JobId jobId)
        {
            return AzureUtil.NormalizeKey(jobId.Name, '_');
        }

        public static string GetRowKey(BuildId buildId)
        {
            return buildId.Id.ToString("0000000000");
        }

        public static EntityKey GetEntityKey(BuildId buildId)
        {
            return new EntityKey(
                GetPartitionKey(buildId.JobId),
                GetRowKey(buildId));
        }
    }
}
