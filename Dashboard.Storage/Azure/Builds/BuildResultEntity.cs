using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure.Builds
{
    /// <summary>
    /// Information about a build result.  The BuildId is unique to this entity irrespective of 
    /// how it is stored.
    /// </summary>
    public sealed class BuildResultEntity : TableEntity
    {
        public string JobName { get; set; }
        public string JobKind { get; set; }
        public string ViewName { get; set; }
        public int BuildNumber { get; set; }
        public string ClassificationKindRaw { get; set; }
        public string ClassificationName { get; set; }
        public string ClassificationDetailedName { get; set; }
        public int DurationSeconds { get; set; }
        public DateTime BuildDateTime { get; set; }
        public string MachineName { get; set; }
        public bool IsPullRequest { get; set; }
        public int PullRequestId { get; set; }
        public string PullRequestAuthor { get; set; }
        public string PullRequestAuthorEmail { get; set; }
        public string PullRequestUrl { get; set; }
        public string PullRequestSha1 { get; set; }

        public DateTimeOffset BuildDateTimeOffset => new DateTimeOffset(BuildDateTime);
        public JobId JobId => JobId.ParseName(JobName);
        public BuildId BuildId => new BuildId(BuildNumber, JobId);
        public ClassificationKind ClassificationKind => (ClassificationKind)Enum.Parse(typeof(ClassificationKind), ClassificationKindRaw ?? ClassificationKind.Unknown.ToString());
        public BuildResultClassification Classification => new BuildResultClassification(ClassificationKind, ClassificationName, ClassificationDetailedName);
        public bool HasPullRequestInfo =>
            PullRequestId != 0 &&
            PullRequestAuthor != null &&
            PullRequestAuthorEmail != null &&
            PullRequestUrl != null &&
            PullRequestSha1 != null;
        public PullRequestInfo PullRequestInfo => HasPullRequestInfo
            ? new PullRequestInfo(author: PullRequestAuthor, authorEmail: PullRequestAuthorEmail, id: PullRequestId, pullUrl: PullRequestUrl, sha1: PullRequestSha1)
            : null;
        public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

        public BuildResultEntity()
        {

        }

        public BuildResultEntity(
            BuildId buildId,
            DateTimeOffset buildDateTime,
            TimeSpan duration,
            string jobKind,
            string machineName,
            BuildResultClassification classification,
            PullRequestInfo prInfo)
        {
            JobName = buildId.JobId.Name;
            JobKind = jobKind;
            ViewName = AzureUtil.GetViewName(BuildId.JobId);
            BuildNumber = buildId.Number;
            ClassificationKindRaw = classification.Kind.ToString();
            ClassificationName = classification.Name;
            BuildDateTime = buildDateTime.UtcDateTime;
            MachineName = machineName;
            IsPullRequest = JobUtil.IsPullRequestJobName(buildId.JobId);
            DurationSeconds = (int)duration.TotalSeconds;

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

            Debug.Assert(BuildDateTime.Kind == DateTimeKind.Utc);
        }

        public BuildResultEntity(BuildResultEntity other) : this(
            buildId: other.BuildId,
            buildDateTime: other.BuildDateTimeOffset,
            duration: other.Duration,
            jobKind: other.JobKind,
            machineName: other.MachineName,
            classification: other.Classification,
            prInfo: other.PullRequestInfo)
        {

        }

        public BuildResultEntity CopyDate()
        {
            var entity = new BuildResultEntity(this);
            entity.SetEntityKey(GetDateEntityKey(BuildDateTimeOffset, BuildId));
            return entity;
        }

        public BuildResultEntity CopyExact()
        {
            var entity = new BuildResultEntity(this);
            entity.SetEntityKey(GetExactEntityKey(BuildId));
            return entity;
        }

        public static EntityKey GetExactEntityKey(BuildId buildId)
        {
            var partitionKey = AzureUtil.NormalizeKey(buildId.JobId.Name, '_');
            var rowKey = buildId.Number.ToString("0000000000");
            return new EntityKey(partitionKey, rowKey);
        }

        public static EntityKey GetDateEntityKey(DateTimeOffset buildDate, BuildId buildId)
        {
            return new EntityKey(
                new DateKey(buildDate).Key,
                new BuildKey(buildId).Key);
        }
    }
}
