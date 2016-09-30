using System;

namespace Dashboard.Jenkins
{
    public sealed class JobId : IEquatable<JobId>
    {
        public static readonly JobId Root = new JobId("");

        public string ShortName { get; }
        public string Name => (IsRoot || Parent.IsRoot) ? ShortName : $"{Parent.Name}/{ShortName}";
        public JobId Parent { get; }
        public bool IsRoot => Root == this;

        public JobId(string shortName, JobId parent = null)
        {
            ShortName = shortName;
            Parent = parent ?? Root;
        }

        public static JobId ParseName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return Root;
            }

            var parts = name.Split('/');
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
                left.ShortName == right.ShortName &&
                left.Parent == right.Parent;
        }

        public static bool operator!=(JobId left, JobId right) => !(left == right);
        public bool Equals(JobId other) => this == other;
        public override string ToString() => Name;
        public override int GetHashCode() => Name.GetHashCode();
        public override bool Equals(object obj) => Equals(obj as JobId);
    }
}
