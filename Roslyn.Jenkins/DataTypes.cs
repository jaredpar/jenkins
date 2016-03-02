using System;
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
        public DateTime Date { get; }
        public TimeSpan Duration { get; }

        public BuildInfo(BuildId id, BuildState state, DateTime date, TimeSpan duration)
        {
            Id = id;
            State = state;
            Date = date;
            Duration = duration;
        }

        public override string ToString()
        {
            return $"{Id} {State}";
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

        public override string ToString() => $"{Id} - {Name}";
    }

    public sealed class ViewInfo
    {
        public string Name { get; }
        public Uri Url { get; }

        public ViewInfo(string name, Uri url)
        {
            Name = name;
            Url = url;
        }

        public override string ToString() => $"{Name} {Url}";
    }

    public sealed class ComputerInfo
    {
        public string Name { get; }
        public string OperatingSystem { get; }

        public ComputerInfo(string name, string operatingSystem)
        {
            Name = name;
            OperatingSystem = operatingSystem;
        }

        public override string ToString() => $"{Name} {OperatingSystem}";
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
        private readonly BuildInfo buildInfo;
        private readonly GetBuildFailureInfo _failureInfo;

        public int Id => buildInfo.Id.Id;
        public BuildId BuildId => buildInfo.Id;
        public BuildInfo BuildInfo => buildInfo;
        public BuildState State => buildInfo.State;
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

        public BuildResult(BuildInfo buildInfo)
        {
            Debug.Assert(buildInfo.State != BuildState.Failed);
            this.buildInfo = buildInfo;
        }

        public BuildResult(BuildInfo buildInfo, GetBuildFailureInfo failureInfo)
        {
            this.buildInfo = buildInfo;
            _failureInfo = failureInfo;
        }
    }

    public enum BuildFailureReason
    {
        Unknown,
        TestCase,
        Build,
        NuGet,
        Infrastructure,
    }

    public sealed class GetBuildFailureInfo
    {
        public static readonly GetBuildFailureInfo Unknown = new GetBuildFailureInfo(BuildFailureReason.Unknown);

        public BuildFailureReason Reason;
        public List<string> Messages;

        public GetBuildFailureInfo(BuildFailureReason reason, List<string> messages = null)
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
