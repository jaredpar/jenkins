using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Dashboard.Jenkins
{
    public static class JobKind
    {
        public const string Normal = "freeStyleProject";
        public const string Folder = "folder";
        public const string Flow = "buildFlow";
        public const string Html = "html";

        public static string[] All => new []
        {
            Normal,
            Folder,
            Flow,
            Html
        };

        public static bool IsWellKnown(string kind)
        {
            switch (kind)
            {
                case Normal:
                case Folder:
                case Flow:
                case Html:
                    return true;
                default:
                    return false;
            }
        }
    }

    public struct JobInfo
    {
        public JobId Id { get; }
        public string JobKind { get; }
        public List<BuildId> Builds { get; }
        public List<JobId> Jobs { get; }

        public JobInfo(JobId id, string jobKind, List<BuildId> builds = null, List<JobId> jobs = null)
        {
            Debug.Assert(jobs.All(x => x.Parent.Name == id.Name));
            builds = builds ?? new List<BuildId>(capacity: 0);
            jobs = jobs ?? new List<JobId>(capacity: 0);

            Id = id;
            Builds = builds;
            Jobs = jobs;
            JobKind = jobKind;
        }

        public override string ToString() => $"{Id} {JobKind}";
    }
}
