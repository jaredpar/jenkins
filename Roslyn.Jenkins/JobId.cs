using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public sealed class JobId : IEquatable<JobId>
    {
        public static readonly JobId Root = new JobId("");

        public string FullName => (IsRoot || Parent.IsRoot) ? Name : $"{Parent.FullName}/{Name}";
        public string Name { get; }
        public JobId Parent { get; }
        public bool IsRoot => Root == this;

        public JobId(string name, JobId parent = null)
        {
            Name = name;
            Parent = parent ?? Root;
        }

        public static JobId ParseFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return Root;
            }

            var parts = fullName.Split('/');
            var current = Root;
            foreach (var part in parts)
            {
                current = new JobId(part, current);
            }

            return current;
        }

        public static bool operator==(JobId left, JobId right)
        {
            if ((object)left == null)
            {
                return (object)right == null;
            }

            if ((object)right == null)
            {
                return false;
            }

            return
                left.Name == right.Name &&
                left.Parent == right.Parent;
        }

        public static bool operator!=(JobId left, JobId right) => !(left == right);
        public bool Equals(JobId other) => this == other;
        public override string ToString() => Parent.IsRoot ? Name : $"{Parent}/{Name}";
        public override int GetHashCode() => Name.GetHashCode();
        public override bool Equals(object obj) => Equals(obj as JobId);
    }
}
