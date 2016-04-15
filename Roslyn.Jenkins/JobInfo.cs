using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public enum JobKind
    {
        Root,
        Job,
        Folder
    }

    public struct JobInfo
    {
        public static readonly JobInfo Root = new JobInfo(JobId.Root, JobKind.Root);

        public JobId Id { get; }
        public JobKind Kind { get; }

        public JobInfo(JobId id, JobKind kind)
        {
            Id = id;
            Kind = kind;
        }

        public override string ToString() => $"{Id} {Kind}";
    }
}
