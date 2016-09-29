using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Dashboard.Azure
{
    public enum ColumnOperator
    {
        LessThan,
        LessThanOrEqual,
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
    }

    public enum CombineOperator
    {
        And,
        Or
    }

    public enum ColumnName
    {
        PartitionKey,
        RowKey
    }

    /// <summary>
    /// Helper type for building up queries via <see cref="TableQuery"/>.  This type is geared towards letting 
    /// IntelliSense help with the queries by using enums and method name guidance. 
    /// </summary>
    public static partial class TableQueryUtil
    {
        public static string PartitionKey(string value, ColumnOperator op = ColumnOperator.Equal) => Column(ColumnName.PartitionKey, value, op);

        public static string RowKey(string value, ColumnOperator op = ColumnOperator.Equal) => Column(ColumnName.RowKey, value, op);

        public static string Key(EntityKey key) => And(PartitionKey(key.PartitionKey), RowKey(key.RowKey));

        public static string Combine(string left, CombineOperator op, string right) => TableQuery.CombineFilters(left, ToTableOperator(op), right);

        public static string Column(ColumnName columnName, string value, ColumnOperator op = ColumnOperator.Equal) => Column(ToColumnName(columnName), value, op);

        public static string And(string left, string right) => Combine(left, CombineOperator.And, right);

        public static string And(string left, string right, params string[] rest)
        {
            var filter = And(left, right);
            foreach (var item in rest)
            {
                filter = And(filter, item);
            }

            return filter;
        }

        public static string Or(string left, string right) => Combine(left, CombineOperator.Or, right);

        public static string Or(string left, string right, params string[] rest)
        {
            var filter = Or(left, right);
            foreach (var item in rest)
            {
                filter = Or(filter, item);
            }

            return filter;
        }

        public static string ToQueryComparison(ColumnOperator op)
        {
            switch (op)
            {
                case ColumnOperator.Equal: return QueryComparisons.Equal;
                case ColumnOperator.NotEqual: return QueryComparisons.NotEqual;
                case ColumnOperator.LessThan: return QueryComparisons.LessThan;
                case ColumnOperator.LessThanOrEqual: return QueryComparisons.LessThanOrEqual;
                case ColumnOperator.GreaterThan: return QueryComparisons.GreaterThan;
                case ColumnOperator.GreaterThanOrEqual: return QueryComparisons.GreaterThanOrEqual;
                default: throw new Exception($"Invalid {nameof(ColumnOperator)} value: {op}");
            }
        }

        public static string ToColumnName(ColumnName name)
        {
            switch (name)
            {
                case ColumnName.PartitionKey: return nameof(TableEntity.PartitionKey);
                case ColumnName.RowKey: return nameof(TableEntity.RowKey);
                default: throw new Exception($"Invalid {nameof(ColumnName)} value: {name}");
            }
        }

        public static string ToTableOperator(CombineOperator op)
        {
            switch (op)
            {
                case CombineOperator.And: return TableOperators.And;
                case CombineOperator.Or: return TableOperators.Or;
                default: throw new InvalidOperationException($"Invalid {nameof(CombineOperator)} value {op}");
            }
        }

    }
}
