using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public static class Extensions
    {
        public static EntityKey GetEntityKey(this ITableEntity table)
        {
            return new EntityKey(table.PartitionKey, table.RowKey);
        }

        public static void SetEntityKey(this ITableEntity table, EntityKey key)
        {
            table.PartitionKey = key.PartitionKey;
            table.RowKey = key.RowKey;
        }

        public static TableQuery<T> Where<T>(this TableQuery<T> query, FilterUtil filter)
        {
            return query.Where(filter.Filter);
        }
    }
}
