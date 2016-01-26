﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public sealed class BuildInfo
    {
        public BuildId Id { get; }
        public BuildState State { get; }
        public string Sha { get; }
        public DateTime Date { get; }
        public TimeSpan Duration { get; }

        public BuildInfo(BuildId id, BuildState state, string sha1, DateTime date, TimeSpan duration)
        {
            Id = id;
            State = state;
            Date = date;
            Sha = sha1;
            Duration = duration;
        }

        public override string ToString()
        {
            return $"{Id} {State} {Sha}";
        }
    }

    public struct BuildId
    {
        public int Id { get; }
        public string Name { get; }

        public BuildId(int id, string name)
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

    public enum BuildState
    {
        Succeeded,
        Failed,
        Aborted,
        Running,
    }

    public sealed class BuildResult
    {
        private readonly BuildInfo _jobInfo;
        private readonly GetBuildFailureInfo _failureInfo;

        public int Id => _jobInfo.Id.Id;
        public BuildId BuildId => _jobInfo.Id;
        public BuildInfo JobInfo => _jobInfo;
        public BuildState State => _jobInfo.State;
        public bool Succeeded => State == BuildState.Succeeded;
        public bool Failed => State == BuildState.Failed;
        public bool Running => State == BuildState.Running;
        public bool Aborted => State == BuildState.Aborted;

        public GetBuildFailureInfo FailureInfo
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

        public BuildResult(BuildInfo jobInfo)
        {
            Debug.Assert(jobInfo.State != BuildState.Failed);
            _jobInfo = jobInfo;
        }

        public BuildResult(BuildInfo jobInfo, GetBuildFailureInfo failureInfo)
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

    public sealed class GetBuildFailureInfo
    {
        public static readonly GetBuildFailureInfo Unknown = new GetBuildFailureInfo(JobFailureReason.Unknown);

        public JobFailureReason Reason;
        public List<string> Messages;

        public GetBuildFailureInfo(JobFailureReason reason, List<string> messages = null)
        {
            Reason = reason;
            Messages = messages ?? new List<string>();
        }
    }

    public sealed class RetestInfo
    {
        public BuildId BuildId { get; }
        public string Sha { get; }
        public bool Handled { get; }
        public string Note { get; }

        public RetestInfo(BuildId buildId, string sha, bool handled, string note = null)
        {
            BuildId = buildId;
            Sha = sha;
            Handled = handled;
            Note = note ?? string.Empty;
        }
    }

    /// <summary>
    /// Information about an item in the Jenkins queue that has not yet been run. 
    /// </summary>
    public sealed class QueuedItemInfo
    {
        public int Id { get; }
        public string JobName { get; }
        public PullRequestInfo PullRequestInfo { get; }

        public QueuedItemInfo(int id, string jobName, PullRequestInfo prInfo)
        {
            Id = id;
            JobName = jobName;
            PullRequestInfo = prInfo;
        }

        public override string ToString()
        {
            if (PullRequestInfo == null)
            {
                return $"{JobName} - {Id}";
            }
            else
            {
                return $"{JobName} - {Id} - {PullRequestInfo.AuthorEmail}";
            }
        }
    }
}
