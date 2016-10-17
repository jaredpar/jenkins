using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    /// <summary>
    /// This is the preferred method for creating <see cref="CounterUtil{T}"/> instances.  It holds state that
    /// makes the underlying counters produce tighter rows. Best practice is to hold one of these in a static and 
    /// create utils on demand from it.
    ///
    /// It is safe to invoke methods on instances of this type in parallel.
    /// </summary>
    public sealed class CounterUtilFactory
    {
        private readonly ConcurrentDictionary<Uri, object> _map = new ConcurrentDictionary<Uri, object>();

        public CounterUtil<T> Create<T>(CloudTableClient client, string tableName)
            where T : class, ITableEntity, new()
        {
            return Create<T>(client.GetTableReference(tableName));
        }

        public CounterUtil<T> Create<T>(CloudTable table)
            where T : class, ITableEntity, new()
        {
            ConcurrentStack<T> stack = null;
            object value;
            if (_map.TryGetValue(table.Uri, out value))
            {
                stack = value as ConcurrentStack<T>;
            }

            if (stack == null)
            {
                stack = new ConcurrentStack<T>();
                _map[table.Uri] = stack;
            }

            return new CounterUtil<T>(table, stack);
        }
    }
}
