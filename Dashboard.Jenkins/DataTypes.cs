using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Dashboard.Jenkins
{
    public sealed class BuildInfo
    {
        public BoundBuildId Id { get; }
        public BuildState State { get; }
        public DateTimeOffset Date { get; }
        public TimeSpan Duration { get; }
        public string MachineName { get; }

        public Uri Host => Id.Host;
        public BuildId BuildId => Id.BuildId;
        public int BuildNumber => Id.Number;
        public JobId JobId => Id.JobId;

        public BuildInfo(BoundBuildId id, BuildState state, DateTimeOffset date, TimeSpan duration, string machineName)
        {
            Id = id;
            State = state;
            Date = date;
            Duration = duration;
            MachineName = machineName;
        }

        public override string ToString()
        {
            return $"{Id} {State}";
        }
    }

    public struct BuildId : IEquatable<BuildId>
    {
        public int Number { get; }
        public JobId JobId { get; }
        public string JobName => JobId.Name;

        public BuildId(int number, JobId jobId)
        {
            Number = number;
            JobId = jobId;
        }

        public static bool operator ==(BuildId left, BuildId right) => left.Number == right.Number && left.JobId == right.JobId;
        public static bool operator !=(BuildId left, BuildId right) => !(left == right);
        public bool Equals(BuildId other) => this == other;
        public override bool Equals(object obj) => obj is BuildId && Equals((BuildId)obj);
        public override int GetHashCode() => Number ^ JobId.GetHashCode();
        public override string ToString() => $"{Number} - {JobName}";
    }

    /// <summary>
    /// A <see cref="BuildId"/> which is bound to a specific Jenkins server.
    /// </summary>
    public struct BoundBuildId : IEquatable<BoundBuildId>
    {
        public Uri Host { get; }
        public BuildId BuildId { get; }

        public int Number => BuildId.Number;
        public JobId JobId => BuildId.JobId;
        public string JobName => BuildId.JobName;
        public Uri BuildUri => GetBuildUri();

        public BoundBuildId(Uri host, BuildId buildId)
        {
            Host = NormalizeHostUri(host);
            BuildId = buildId;
        }

        public BoundBuildId(Uri host, int number, JobId id) : this(host, new BuildId(number, id))
        {

        }

        public Uri GetBuildUri(bool? useHttps = null)
        {
            var builder = new UriBuilder(Host);
            if (useHttps == true)
            {
                builder.Scheme = Uri.UriSchemeHttps;
            }

            builder.Path = JenkinsUtil.GetBuildPath(BuildId);
            return builder.Uri;
        }

        public static bool TryParse(Uri uri, out BoundBuildId boundBuildId)
        {
            BuildId buildId;
            if (!JenkinsUtil.TryConvertPathToBuildId(uri.PathAndQuery, out buildId))
            {
                boundBuildId = default(BoundBuildId);
                return false;
            }

            boundBuildId = new BoundBuildId(uri, buildId);
            return true;
        }

        public static BoundBuildId Parse(string uri)
        {
            var realUri = new Uri(uri);
            BoundBuildId boundBuildId;
            if (!TryParse(realUri, out boundBuildId))
            {
                throw new Exception($"Not a valid build uri: {uri}");
            }

            return boundBuildId;
        }

        /// <summary>
        /// The host URI needs only the scheme, authority portions of the URI.  Everything else should be stripped to 
        /// ensure the value is normalized.
        ///
        /// TODO: move to JenkinsUtil
        /// </summary>
        public static Uri NormalizeHostUri(Uri uri)
        {
            if (string.IsNullOrEmpty(uri.PathAndQuery) && uri.Host.All(Char.IsLower))
            {
                return uri;
            }

            var builder = new UriBuilder();
            builder.Scheme = uri.Scheme;
            builder.Host = uri.Host.ToLower();
            builder.Port = uri.Port;
            return builder.Uri;
        }

        public static bool operator==(BoundBuildId left, BoundBuildId right) => 
            left.Host == right.Host &&
            left.BuildId == right.BuildId;
        public static bool operator!=(BoundBuildId left, BoundBuildId right) => !(left == right);
        public bool Equals(BoundBuildId other) => this == other;
        public override bool Equals(object obj) => obj is BoundBuildId && Equals((BoundBuildId)obj);
        public override int GetHashCode() => BuildId.GetHashCode();
        public override string ToString() => GetBuildUri(useHttps: false).ToString();
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
        public string Author { get; }
        public string AuthorEmail { get; }
        public int Id { get; }
        public string PullUrl { get; }
        public string Sha1 { get; }

        public PullRequestInfo(string author, string authorEmail, int id, string pullUrl, string sha1)
        {
            Author = author;
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

        public int Id => buildInfo.Id.Number;
        public BoundBuildId BoundBuildId => buildInfo.Id;
        public BuildId BuildId => buildInfo.BuildId;
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

    public struct BuildFailureCause
    {
        public const string CategoryUnknown = "Unknown";
        public const string CategoryMergeConflict = "Merge Conflict";
        public const string CategoryTest = "Test";

        public static readonly BuildFailureCause Unknown = new BuildFailureCause(name: "", description: "", category: CategoryUnknown);
        public static readonly BuildFailureCause MergeConflict = new BuildFailureCause(name: "", description: "", category: CategoryMergeConflict);

        public string Name { get; }
        public string Description { get; }
        public string Category { get; }

        public BuildFailureCause(string name, string description, string category)
        {
            Name = name;
            Description = description;
            Category = category;
        }

        public override string ToString() => $"{Name} -> {Category}";
    }

    public sealed class BuildFailureInfo
    {
        public static readonly BuildFailureInfo Unknown = new BuildFailureInfo(BuildFailureCause.Unknown);

        public ReadOnlyCollection<BuildFailureCause> CauseList { get; }

        public BuildFailureInfo(BuildFailureCause cause) : this(new ReadOnlyCollection<BuildFailureCause>(new[] { cause }))
        {

        }

        public BuildFailureInfo(ReadOnlyCollection<BuildFailureCause> causeList)
        {
            Debug.Assert(causeList.Count > 0);
            CauseList = causeList;
        }
    }

    /// <summary>
    /// Information about an item in the Jenkins queue that has not yet been run. 
    /// </summary>
    public sealed class QueuedItemInfo
    {
        public int Id { get; }
        public JobId JobId { get; }
        public PullRequestInfo PullRequestInfo { get; }
        public int? BuildNumber { get; }

        public QueuedItemInfo(int id, JobId jobId, PullRequestInfo prInfo, int? buildNumber)
        {
            Id = id;
            JobId = jobId;
            PullRequestInfo = prInfo;
            BuildNumber = buildNumber;
        }

        public override string ToString()
        {
            if (PullRequestInfo == null)
            {
                return $"{JobId.Name} - {Id}";
            }
            else
            {
                return $"{JobId.Name} - {Id} - {PullRequestInfo.AuthorEmail}";
            }
        }
    }
}
