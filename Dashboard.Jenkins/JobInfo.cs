using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Jenkins
{
    /// <summary>
    /// What type of job is this?
    /// </summary>
    public enum JobKind
    {
        /// <summary>
        /// Job that runs builds.
        /// </summary>
        Build,

        /// <summary>
        /// Job that just contains other jobs.
        /// </summary>
        Folder,

        /// <summary>
        /// Job which is just a composition of other jobs.
        /// </summary>
        Flow
    }

    public struct JobInfo
    {
        public JobId Id { get; }
        public JobKind Kind { get; }
        public List<BuildId> Builds { get; }
        public List<JobId> Jobs { get; }

        public JobInfo(JobId id, JobKind kind, List<BuildId> builds = null, List<JobId> jobs = null)
        {
            Debug.Assert(jobs.All(x => x.Parent.Name == id.Name));
            builds = builds ?? new List<BuildId>(capacity: 0);
            jobs = jobs ?? new List<JobId>(capacity: 0);

            Id = id;
            Builds = builds;
            Jobs = jobs;
            Kind = kind;
        }

        public override string ToString() => $"{Id} {Kind}";
    }
}
