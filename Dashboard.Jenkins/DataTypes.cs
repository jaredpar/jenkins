﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Jenkins
{
    public sealed class BuildInfo
    {
        public BuildId Id { get; }
        public BuildState State { get; }
        public DateTimeOffset Date { get; }
        public TimeSpan Duration { get; }
        public string MachineName { get; }

        public BuildInfo(BuildId id, BuildState state, DateTimeOffset date, TimeSpan duration, string machineName)
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

    // TODO: equality
    public struct BuildId
    {
        // TODO: rename to number? 
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
