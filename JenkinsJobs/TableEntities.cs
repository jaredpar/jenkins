using Microsoft.WindowsAzure.Storage.Table;
using Roslyn.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JenkinsJobs
{
    public sealed class BuildProcessedEntity : TableEntity
    {
        public bool Succeeded { get; set; }

        public BuildId BuildId => new BuildId(id: int.Parse(RowKey), jobName: PartitionKey);

        public BuildProcessedEntity()
        {

        }

        public BuildProcessedEntity(BuildId buildId, bool succeeded) : base(buildId.JobName, buildId.Id.ToString())
        {
            Succeeded = succeeded;
        }
    }

    public enum BuildFailureKind
    {
        Unknown = 0,
        UnitTest = 1,
    }

    public sealed class BuildFailureEntity : TableEntity
    {
        public BuildFailureKind Kind { get; set; }
        public string Extra { get; set; }

        public BuildFailureEntity()
        {

        }

        private BuildFailureEntity(BuildId buildId, BuildFailureKind kind, string rowKey) : base($"{buildId.JobName} {buildId.Id}", rowKey)
        {
            Kind = kind;
        }

        public static BuildFailureEntity CreateUnitTestFailure(BuildId buildId, string testCaseName, string extra = "")
        {
            return new BuildFailureEntity(buildId, BuildFailureKind.UnitTest, rowKey: testCaseName)
            {
                Extra = extra
            };
        }
    }
}
