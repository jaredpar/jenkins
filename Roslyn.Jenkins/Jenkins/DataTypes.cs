using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public sealed class JobInfo
    {
        public readonly JobId Id;
        public readonly JobState State;
        public readonly DateTime Date;
        public readonly string Sha;

        public JobInfo(JobId id, JobState state, string sha1, DateTime date)
        {
            Id = id;
            State = state;
            Date = date;
            Sha = sha1;
        }

        public override string ToString()
        {
            return $"{Id} {State} {Sha}";
        }
    }

    public struct JobId
    {
        public int Id { get; }
        public string Name { get; }

        public JobId(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return $"{Id} - {Name}";
        }
    }

    public sealed class PullRequestInfo
    {
        public string AuthorEmail { get; }
        public int Id { get; }
        public string PullUrl { get; }
        public string Sha1 { get; }

        public PullRequestInfo(string authorEmail, int id, string pullUrl, string sha1)
        {
            AuthorEmail = authorEmail;
            Id = id;
            PullUrl = pullUrl;
            Sha1 = sha1;
        }

        public override string ToString()
        {
            return $"{PullUrl} - {AuthorEmail}";
        }
    }

    /// <summary>
    /// The key which uniquely identifies a test asset.  Anytime this appears more than once
    /// in the set of job infos then the same set of changes were run twice through Jenkins
    /// </summary>
    public struct BuildKey
    {
        public readonly int PullId;
        public readonly string Sha1;

        public BuildKey(int pullId, string sha1)
        {
            PullId = pullId;
            Sha1 = sha1;
        }

        public override string ToString()
        {
            return $"{PullId} - {Sha1}";
        }
    }

    public enum JobState
    {
        Succeeded,
        Failed,
        Aborted,
        Running,
    }

    public sealed class JobResult
    {
        private readonly JobInfo _jobInfo;
        private readonly JobFailureInfo _failureInfo;

        public int Id => _jobInfo.Id.Id;
        public JobId JobId => _jobInfo.Id;
        public JobInfo JobInfo => _jobInfo;
        public JobState State => _jobInfo.State;
        public bool Succeeded => State == JobState.Succeeded;
        public bool Failed => State == JobState.Failed;
        public bool Running => State == JobState.Running;
        public bool Aborted => State == JobState.Aborted;

        public JobFailureInfo FailureInfo
        {
            get
            {
                if (!Failed)
                {
                    throw new InvalidOperationException();
                }

                return _failureInfo;
            }
        }

        public JobResult(JobInfo jobInfo)
        {
            Debug.Assert(jobInfo.State != JobState.Failed);
            _jobInfo = jobInfo;
        }

        public JobResult(JobInfo jobInfo, JobFailureInfo failureInfo)
        {
            _jobInfo = jobInfo;
            _failureInfo = failureInfo;
        }
    }

    public enum JobFailureReason
    {
        Unknown,
        TestCase,
        Build,
        NuGet,
        Infrastructure,
    }

    public sealed class JobFailureInfo
    {
        public static readonly JobFailureInfo Unknown = new JobFailureInfo(JobFailureReason.Unknown);

        public JobFailureReason Reason;
        public List<string> Messages;

        public JobFailureInfo(JobFailureReason reason, List<string> messages = null)
        {
            Reason = reason;
            Messages = messages ?? new List<string>();
        }
    }

    public sealed class RetestInfo
    {
        public JobId JobId { get; }
        public string Sha { get; }
        public bool Handled { get; }
        public string Note { get; }

        public RetestInfo(JobId jobId, string sha, bool handled, string note = null)
        {
            JobId = jobId;
            Sha = sha;
            Handled = handled;
            Note = note ?? string.Empty;
        }
    }
}
