using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public struct EntityKey : IEquatable<EntityKey>
    {
        public string PartitionKey { get; }
        public string RowKey { get; }

        public EntityKey(string partitionKey, string rowKey)
        {
            Debug.Assert(!AzureUtil.IsIllegalKey(partitionKey));
            Debug.Assert(!AzureUtil.IsIllegalKey(rowKey));
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public static bool operator==(EntityKey left, EntityKey right)
        {
            return
                left.PartitionKey == right.PartitionKey &&
                left.RowKey == right.RowKey;
        }

        public static bool operator!=(EntityKey left, EntityKey right)
        {
            return !(left == right);
        }

        public bool Equals(EntityKey other) => this == other;
        public override bool Equals(object obj) => obj is EntityKey && Equals((EntityKey)obj);
        public override int GetHashCode() => PartitionKey.GetHashCode();
        public override string ToString() => $"P:{PartitionKey} R:{RowKey}";
    }
}
