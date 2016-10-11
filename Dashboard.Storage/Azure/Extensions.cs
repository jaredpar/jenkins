using Microsoft.WindowsAzure.Storage.Table;
using System;

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

        public static void Execute<T>(this TableBatchOperation operation, TableOperationType type, T entity)
            where T : ITableEntity
        {
            switch (type)
            {
                case TableOperationType.Insert:
                    operation.Insert(entity);
                    break;
                case TableOperationType.Delete:
                    operation.Delete(entity);
                    break;
                case TableOperationType.Replace:
                    operation.Replace(entity);
                    break;
                case TableOperationType.Merge:
                    operation.Merge(entity);
                    break;
                case TableOperationType.InsertOrReplace:
                    operation.InsertOrReplace(entity);
                    break;
                case TableOperationType.InsertOrMerge:
                    operation.InsertOrMerge(entity);
                    break;
                case TableOperationType.Retrieve:
                    operation.Retrieve(entity.PartitionKey, entity.RowKey);
                    break;
                default:
                    throw new Exception($"Invalid enum value {nameof(TableOperationType)} {type}");
            }
        }
    }
}
