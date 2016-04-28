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

    /// <summary>
    /// Base type for tracking build failure information.  The combination of identifier and 
    /// <see cref="BuildId"/> should always be unique.  This entity is stored in a number of tables
    /// based on different Partition / Row keys.
    /// </summary>
    public sealed class BuildFailureEntity : TableEntity, ICopyableTableEntity<BuildFailureEntity>
    {
        public string BuildFailureKindRaw { get; set; }
        public DateTime BuildDate { get; set; }
        public string JobName { get; set; }
        public int BuildNumber { get; set; }
        public string Identifier { get; set; }

        public JobId JobId => JobId.ParseName(JobName);
        public BuildId BuildId => new BuildId(BuildNumber, JobId);
        public BuildFailureKind BuildFailureKind => (BuildFailureKind)Enum.Parse(typeof(BuildFailureKind), BuildFailureKindRaw);

        public BuildFailureEntity()
        {

        }

        public BuildFailureEntity(BuildFailureEntity other) : this(
            buildDate: other.BuildDate,
            buildId: other.BuildId,
            identifier: other.Identifier,
            kind: other.BuildFailureKind)
        {

        }

        public BuildFailureEntity(DateTimeOffset buildDate, BuildId buildId, string identifier, BuildFailureKind kind)
        {
            JobName = buildId.JobName;
            BuildNumber = buildId.Id;
            Identifier = identifier;
            BuildFailureKindRaw = kind.ToString();
            BuildDate = buildDate.UtcDateTime;
        }

        public BuildFailureEntity Copy(EntityKey key)
        {
            var entity = new BuildFailureEntity(this);
            entity.SetEntityKey(key);
            return entity;
        }

        public EntityKey GetDateEntityKey()
        {
            return GetDateEntityKey(BuildDate, BuildId, Identifier);
        }

        public static EntityKey GetDateEntityKey(DateTimeOffset buildDate, BuildId buildId, string identifier)
        {
            return new EntityKey(
                new DateKey(buildDate).Key,
                $"{new BuildKey(buildId)}-{identifier}");
        }

        public EntityKey GetExactEntityKEy()
        {
            return GetExactEntityKey(BuildId, Identifier);
        }

        public static EntityKey GetExactEntityKey(BuildId buildId, string identifier)
        {
            return new EntityKey(
                identifier,
                new BuildKey(buildId).Key);
        }

        public static BuildFailureEntity CreateTestCaseFailure(DateTimeOffset buildDateTime, BuildId buildId, string testCaseName, string machineName)
        {
            return new BuildFailureEntity(
                buildDateTime,
                buildId,
                testCaseName,
                BuildFailureKind.TestCase);
        }
    }
}
