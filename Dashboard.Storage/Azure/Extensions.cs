using Microsoft.WindowsAzure.Storage.Table;

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
    }
}
