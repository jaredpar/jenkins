using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public sealed class JobId
    {
        public static readonly JobId Root = new JobId("");

        public string Name { get; }
        public JobId Parent { get; }
        public bool IsRoot => Root == this;

        public JobId(string name, JobId parent = null)
        {
            Name = name;
            Parent = parent ?? Root;
        }

        public override string ToString() => Parent.IsRoot ? Name : $"{Parent}/{Name}";
    }
}
