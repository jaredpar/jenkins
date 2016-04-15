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
        public JobId JobId { get; }
        public string JobName => JobId.Name;

        public BuildId(int id, JobId jobId)
        {
            Id = id;
            JobId = jobId;
        }

        public override string ToString() => $"{Id} - {JobName}";
    }

    public sealed class ViewInfo
    {
        public string Name { get; }
        public string Description { get; }
        public Uri Url { get; }

        public ViewInfo(string name, string description, Uri url)
        {
            Name = name;
            Description = description;
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
        private readonly BuildFailureInfo _failureInfo;

        public int Id => buildInfo.Id.Id;
        public BuildId BuildId => buildInfo.Id;
        public BuildInfo BuildInfo => buildInfo;
        public BuildState State => buildInfo.State;
        public bool Succeeded => State == BuildState.Succeeded;
        public bool Failed => State == BuildState.Failed;
        public bool Running => State == BuildState.Running;
        public bool Aborted => State == BuildState.Aborted;

        public BuildFailureInfo FailureInfo
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

        public BuildResult(BuildInfo buildInfo, BuildFailureInfo failureInfo)
        {
            this.buildInfo = buildInfo;
            _failureInfo = failureInfo;
        }
    }

    // TODO: Category should be a string in the non-Roslyn version.  It's only an enum in Roslyn where we 
    // have the context.
    public enum BuildFailureCategory
    {
        Unknown,
        TestCase,
        Build,
        NuGet,
        Infrastructure,
        MergeConflict,
    }

    public sealed class BuildFailureInfo
    {
        public static readonly BuildFailureInfo Unknown = new BuildFailureInfo(name: "", description: "", category: BuildFailureCategory.Unknown);
        public static readonly BuildFailureInfo MergeConflict = new BuildFailureInfo(name: "", description: "", category: BuildFailureCategory.MergeConflict);

        public string Name { get; }
        public string Description { get; }
        public BuildFailureCategory Category { get; }

        public BuildFailureInfo(string name, string description, BuildFailureCategory category)
        {
            Name = name;
            Description = description;
            Category = category;
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
