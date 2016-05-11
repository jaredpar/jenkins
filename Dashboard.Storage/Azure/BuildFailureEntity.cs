using Microsoft.WindowsAzure.Storage.Table;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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
    public sealed class BuildFailureEntity : TableEntity
    {
        public string BuildFailureKindRaw { get; set; }
        public DateTime BuildDateTime { get; set; }
        public string JobName { get; set; }
        public int BuildNumber { get; set; }
        public string Identifier { get; set; }
        public string MachineName { get; set; }
        public int PullRequestId { get; set; }
        public string PullRequestAuthor { get; set; }
        public string PullRequestAuthorEmail { get; set; }
        public string PullRequestUrl { get; set; }
        public string PullRequestSha1 { get; set; }

        public JobId JobId => JobId.ParseName(JobName);
        public BuildId BuildId => new BuildId(BuildNumber, JobId);
        public BuildFailureKind BuildFailureKind => (BuildFailureKind)Enum.Parse(typeof(BuildFailureKind), BuildFailureKindRaw);
        public DateTimeOffset BuildDateTimeOffset => BuildDateTime;
        public bool HasPullRequestInfo =>
            PullRequestId != 0 &&
            PullRequestAuthor != null &&
            PullRequestAuthorEmail != null &&
            PullRequestUrl != null &&
            PullRequestSha1 != null;
        public PullRequestInfo PullRequestInfo => HasPullRequestInfo
            ? new PullRequestInfo(author: PullRequestAuthor, authorEmail: PullRequestAuthorEmail, id: PullRequestId, pullUrl: PullRequestUrl, sha1: PullRequestSha1)
            : null;

        public BuildFailureEntity()
        {

        }

        public BuildFailureEntity(BuildFailureEntity other) : this(
            buildId: other.BuildId,
            identifier: other.Identifier,
            buildDate: other.BuildDateTime,
            kind: other.BuildFailureKind,
            machineName: other.MachineName,
            prInfo: other.PullRequestInfo)
        {

        }

        public BuildFailureEntity(BuildId buildId, string identifier, DateTimeOffset buildDate, BuildFailureKind kind, string machineName, PullRequestInfo prInfo)
        {
            JobName = buildId.JobName;
            BuildNumber = buildId.Number;
            Identifier = identifier;
            BuildFailureKindRaw = kind.ToString();
            BuildDateTime = buildDate.UtcDateTime;
            MachineName = machineName;
            if (prInfo != null)
            {
                PullRequestId = prInfo.Id;
                PullRequestAuthor = prInfo.Author;
                PullRequestAuthorEmail = prInfo.AuthorEmail;
                PullRequestUrl = prInfo.PullUrl;
                PullRequestSha1 = prInfo.Sha1;
                Debug.Assert(HasPullRequestInfo);
                Debug.Assert(PullRequestInfo != null);
            }
        }

        public BuildFailureEntity CopyExact()
        {
            var entity = new BuildFailureEntity(this);
            entity.SetEntityKey(GetExactEntityKey(BuildId, Identifier));
            return entity;
        }

        public BuildFailureEntity CopyDate()
        {
            var entity = new BuildFailureEntity(this);
            entity.SetEntityKey(GetDateEntityKey(BuildDateTime, BuildId, Identifier));
            return entity;
        }

        public static EntityKey GetDateEntityKey(DateTimeOffset buildDate, BuildId buildId, string identifier)
        {
            return new EntityKey(
                new DateKey(buildDate).Key,
                $"{new BuildKey(buildId).Key}-{identifier}");
        }

        public static EntityKey GetExactEntityKey(BuildId buildId, string identifier)
        {
            return new EntityKey(
                identifier,
                new BuildKey(buildId).Key);
        }

        public static BuildFailureEntity CreateTestCaseFailure(DateTimeOffset buildDateTime, BuildId buildId, string testCaseName, string machineName, PullRequestInfo prInfo)
        {
            return new BuildFailureEntity(
                buildId,
                testCaseName,
                buildDateTime,
                BuildFailureKind.TestCase,
                machineName: machineName,
                prInfo: prInfo);
        }
    }
}
