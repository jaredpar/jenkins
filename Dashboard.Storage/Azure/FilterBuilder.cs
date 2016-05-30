using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public enum ColumnNames
    {
        PartitionKey,
        RowKey
    }

    public struct FilterUtil
    {
        private readonly string _filter;

        public string Filter => _filter;

        private FilterUtil(string filter)
        {
            _filter = filter;
        }

        public FilterUtil And(FilterUtil other)
        {
            var filter = TableQuery.CombineFilters(
                _filter,
                TableOperators.And,
                other.Filter);
            return new FilterUtil(filter);
        }

        public FilterUtil Or(FilterUtil other)
        {
            var filter = TableQuery.CombineFilters(
                _filter,
                TableOperators.Or,
                other.Filter);
            return new FilterUtil(filter);
        }

        public static FilterUtil PartitionKey(string partitionKey)
        {
            var filter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.PartitionKey),
                QueryComparisons.Equal,
                partitionKey);
            return new FilterUtil(filter);
        }

        public static FilterUtil RowKey(string rowKey)
        {
            var filter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.RowKey),
                QueryComparisons.Equal,
                rowKey);
            return new FilterUtil(filter);
        }

        public static FilterUtil Key(EntityKey key)
        {
            return FilterUtil
                .PartitionKey(key.PartitionKey)
                .And(FilterUtil.RowKey(key.RowKey));
        }

        public static FilterUtil SinceDate(ColumnNames name, DateKey startDate)
        {
            return SinceDate(ToColumnName(name), startDate);
        }

        public static FilterUtil SinceDate(string columnName, DateKey startDate)
        {
            var filter = TableQuery.GenerateFilterCondition(
                columnName,
                QueryComparisons.GreaterThanOrEqual,
                startDate.Key);
            return new FilterUtil(filter);
        }

        public static FilterUtil BetweenDateKeys(ColumnNames name, DateKey startDate, DateKey endDate)
        {
            return BetweenDateKeys(ToColumnName(name), startDate, endDate);
        }

        public static FilterUtil BetweenDateKeys(string columnName, DateKey startDate, DateKey endDate)
        {
            return Combine(
                Column(columnName, startDate, ColumnOperator.GreaterThanOrEqual),
                CombineOperator.And,
                Column(columnName, endDate, ColumnOperator.LessThanOrEqual));
        }

        public static FilterUtil Combine(FilterUtil left, CombineOperator op, FilterUtil right)
        {
            var filter = TableQuery.CombineFilters(
                left.Filter,
                ToTableOperator(op),
                right.Filter);
            return new FilterUtil(filter);
        }

        public static FilterUtil Column(ColumnNames name, DateKey dateKey, ColumnOperator op = ColumnOperator.Equal)
        {
            return Column(ToColumnName(name), dateKey, op);
        }

        public static FilterUtil Column(string columnName, DateKey dateKey, ColumnOperator op = ColumnOperator.Equal)
        {
            return Column(columnName, dateKey.Key, op);
        }

        public static FilterUtil Column(string columnName, string value, ColumnOperator op = ColumnOperator.Equal)
        {
            var filter = TableQuery.GenerateFilterCondition(
                columnName,
                ToQueryComparison(op),
                value);
            return new FilterUtil(filter);
        }

        public static FilterUtil Column(string columnName, int value, ColumnOperator op = ColumnOperator.Equal)
        {
            var filter = TableQuery.GenerateFilterConditionForInt(
                columnName,
                ToQueryComparison(op),
                value);
            return new FilterUtil(filter);
        }

        public static FilterUtil Column(string columnName, bool value, ColumnOperator op = ColumnOperator.Equal)
        {
            var filter = TableQuery.GenerateFilterConditionForBool(
                columnName,
                ToQueryComparison(op),
                value);
            return new FilterUtil(filter);
        }

        public static FilterUtil Column(string columnName, long value, ColumnOperator op = ColumnOperator.Equal)
        {
            var filter = TableQuery.GenerateFilterConditionForLong(
                columnName,
                ToQueryComparison(op),
                value);
            return new FilterUtil(filter);
        }

        public static FilterUtil Column(string columnName, DateTimeOffset value, ColumnOperator op = ColumnOperator.Equal)
        {
            var filter = TableQuery.GenerateFilterConditionForDate(
                columnName,
                ToQueryComparison(op),
                value);
            return new FilterUtil(filter);
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

        public static string ToColumnName(ColumnNames name)
        {
            switch (name)
            {
                case ColumnNames.PartitionKey: return nameof(TableEntity.PartitionKey);
                case ColumnNames.RowKey: return nameof(TableEntity.RowKey);
                default: throw new Exception($"Invalid {nameof(ColumnNames)} value: {name}");
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
