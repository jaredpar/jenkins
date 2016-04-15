using Microsoft.WindowsAzure.Storage.Table;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public enum BuildResultKind
    {
        Succeeded,
        Aborted,
        Running,

        // An unknown failure which was not resolved in a specific time period will 
        // transition to the ignored state.  At this point the web job will no longer 
        // attempt to find a reason for it.
        IgnoredFailure,

        UnknownFailure,
        UnitTestFailure,
        NuGetFailure,
        InfrastructureFailure,
        BuildFailure,
        MergeConflict,
    }

    public sealed class BuildProcessedEntity : TableEntity
    {
        public string KindRaw { get; set; }
        public DateTime BuildDate { get; set; }

        public BuildResultKind Kind => (BuildResultKind)Enum.Parse(typeof(BuildResultKind), KindRaw);
        public BuildId BuildId => new BuildId(id: int.Parse(RowKey), jobId: JobId.ParseName(PartitionKey));

        public BuildProcessedEntity()
        {

        }

        public BuildProcessedEntity(BuildId buildId, DateTime buildDate, BuildResultKind kind) : base(buildId.JobName, buildId.Id.ToString())
        {
            BuildDate = buildDate;
            KindRaw = kind.ToString();
        }
    }
}
