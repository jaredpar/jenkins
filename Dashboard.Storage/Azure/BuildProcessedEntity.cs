using Microsoft.WindowsAzure.Storage.Table;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    /// <summary>
    /// Known reasons that a given build can fail.
    /// </summary>
    public enum BuildResultKind
    {
        Succeeded,
        Aborted,
        Running,

        /// <summary>
        /// There was an error analyzing the build.
        /// </summary>
        AnalyzeError,

        /// <summary>
        /// An unknown failure which was not resolved in a specific time period will 
        /// transition to the ignored state.  At this point the web job will no longer 
        /// attempt to find a reason for it.
        /// </summary>
        IgnoredFailure,

        /// <summary>
        /// Jenkins has not established a cause for this failure.  The code was able to 
        /// process the build correctly, Jenkins is just lacking data.
        /// </summary>
        UnknownFailure,
        UnitTestFailure,
        NuGetFailure,
        InfrastructureFailure,
        BuildFailure,
        MergeConflict,
    }

    /// <summary>
    /// Build result data that is unique on the BuildId.  Similar to <see cref="BuildResultEntity"/>.
    /// </summary>
    public sealed class BuildProcessedEntity : TableEntity
    {
        public const string TableName = AzureConstants.TableNames.BuildProcessed;

        public string KindRaw { get; set; }
        public DateTime BuildDate { get; set; }
        public string MachineName { get; set; }

        public BuildResultKind Kind => (BuildResultKind)Enum.Parse(typeof(BuildResultKind), KindRaw);
        public BuildId BuildId => new BuildId(id: int.Parse(RowKey), jobId: JobId.ParseName(PartitionKey));

        public BuildProcessedEntity()
        {

        }

        public BuildProcessedEntity(BuildId buildId, DateTime buildDate, string machineName, BuildResultKind kind) : base(buildId.JobName, buildId.Id.ToString())
        {
            BuildDate = buildDate;
            KindRaw = kind.ToString();
            MachineName = machineName;
        }

        public static EntityKey GetEntityKey(BuildId buildId)
        {
            return new EntityKey(
                buildId.JobName,
                buildId.Id.ToString());
        }
    }
}
