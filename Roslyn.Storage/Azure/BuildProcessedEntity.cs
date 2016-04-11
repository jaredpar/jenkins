using Microsoft.WindowsAzure.Storage.Table;
using Roslyn.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Azure
{
    public enum BuildResultKind
    {
        Succeeded = 0,
        UnitTestFailure = 1,
        UnknownFailure = 2,
    }

    public sealed class BuildProcessedEntity : TableEntity
    {
        public string KindRaw { get; set; }
        public DateTime BuildDate { get; set; }

        public BuildResultKind Kind => (BuildResultKind)Enum.Parse(typeof(BuildResultKind), KindRaw);
        public BuildId BuildId => new BuildId(id: int.Parse(RowKey), jobName: PartitionKey);

        public BuildProcessedEntity()
        {

        }

        public BuildProcessedEntity(BuildId buildId, DateTime buildDate, BuildResultKind kind) : base(buildId.JobName, buildId.Id.ToString())
        {
            BuildDate = buildDate;
            KindRaw = Kind.ToString();
        }
    }
}
