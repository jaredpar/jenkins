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
    /// Base type holding all of the properties that are common to entities that hold build 
    /// result information.
    /// </summary>
    public abstract class BuildResultEntityBase : TableEntity
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

        protected BuildResultEntityBase()
        {

        }

        protected BuildResultEntityBase(
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

        protected BuildResultEntityBase(BuildResultEntityBase other) : this(
            buildId: other.BuildId,
            buildDateTime: other.BuildDateTimeOffset,
            machineName: other.MachineName,
            kind: other.BuildResultKind)
        {

        }
    }
}
