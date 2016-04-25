using Microsoft.WindowsAzure.Storage.Table;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public enum BuildFailureKind
    {
        Unknown,
        TestCase,
    }

    public sealed class BuildFailureEntity : TableEntity
    {
        public const string TableName = AzureConstants.TableNames.BuildFailure;

        public string KindRaw { get; set; }
        public DateTime BuildDate { get; set; }
        public string Extra { get; set; }
        public string MachineName { get; set; }

        public string Identifier => RowKey;
        public BuildId BuildId => ParseBuildId();
        public BuildFailureKind Kind => (BuildFailureKind)Enum.Parse(typeof(BuildFailureKind), KindRaw);

        public BuildFailureEntity()
        {

        }

        private BuildFailureEntity(BuildId buildId, string rowKey, BuildFailureKind kind, DateTime buildDate) : base($"{buildId.JobName} {buildId.Id}", rowKey)
        {
            KindRaw = kind.ToString();
            BuildDate = buildDate;
        }

        public static BuildFailureEntity CreateTestCaseFailure(BuildId buildId, string testCaseName, DateTime buildDate, string machineName, string extra = "")
        {
            testCaseName = DashboardStorage.NormalizeTestCaseName(testCaseName);
            return new BuildFailureEntity(buildId, kind: BuildFailureKind.TestCase, rowKey: testCaseName, buildDate: buildDate)
            {
                MachineName = machineName,
                Extra = extra
            };
        }

        private BuildId ParseBuildId()
        {
            var index = PartitionKey.LastIndexOf(' ');
            var idStr = PartitionKey.Substring(index);
            var jobStr = PartitionKey.Substring(0, length: index);
            var jobId = JobId.ParseName(jobStr);
            return new BuildId(int.Parse(idStr), jobId);
        }
    }
}
