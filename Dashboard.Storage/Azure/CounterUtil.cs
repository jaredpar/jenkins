using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public static class CounterUtil
    {
        public const char RowKeySeparatorChar = '!';
        public const int MinuteInternal = 15;

        private static readonly char[] s_rowKeySeparatorCharArray = new[] { RowKeySeparatorChar };

        public static EntityKey GetEntityKey(CounterData counterData)
        {
            return new EntityKey(
                GetPartitionKey(counterData.DateTime),
                GetRowKey(counterData));
        }

        public static string GetPartitionKey(DateTime dateTime)
        {
            Debug.Assert(dateTime.Kind == DateTimeKind.Utc);
            var date = dateTime.Date;
            return date.ToString("yyyy-MM-dd");
        }

        public static string GetRowKey(CounterData counterData)
        {
            Debug.Assert(!counterData.EntityWriterId.Contains(RowKeySeparatorChar));
            return $"{counterData.EntityWriterId}{RowKeySeparatorChar}{GetTimeOfDayTicks(counterData.DateTime)}{RowKeySeparatorChar}{counterData.IsJenkins}";
        }

        public static string GetEntityWriterId(string rowKey)
        {
            string entityWriterId;
            long timeOfDayTicks;
            bool isJenkins;
            ParseRowKey(rowKey, out entityWriterId, out timeOfDayTicks, out isJenkins);
            return entityWriterId;
        }

        public static void ParseRowKey(string rowKey, out string entityWriterId, out long timeOfDayTicks, out bool isJenkins)
        {
            var array = rowKey.Split(s_rowKeySeparatorCharArray, count: 3);
            entityWriterId = array[0];
            timeOfDayTicks = long.Parse(array[1]);
            isJenkins = bool.Parse(array[2]);
        }

        /// <summary>
        /// Split a UTC <see cref="DateTime"/> into the components used by this Entity.  The time component
        /// will be adjusted for the interval stored by this table.
        /// </summary>
        public static long GetTimeOfDayTicks(DateTime dateTime)
        {
            Debug.Assert(dateTime.Kind == DateTimeKind.Utc);
            var minute = dateTime.TimeOfDay.Minutes;
            minute = (minute / MinuteInternal) * MinuteInternal;
            var timeOfDay = new TimeSpan(hours: dateTime.TimeOfDay.Hours, minutes: minute, seconds: 0);
            return timeOfDay.Ticks;
        }

        /// <summary>
        /// Query counter entities between the specified dates.
        /// </summary>
        public static List<T> Query<T>(CloudTable table, DateTime startDate, DateTime endDate)
            where T : ITableEntity, new()
        {
            Debug.Assert(startDate.Kind == DateTimeKind.Utc);
            Debug.Assert(endDate.Kind == DateTimeKind.Utc);

            // TODO: what if startdate and end date are the same day???

            var list = new List<T>();
            list.AddRange(QueryStart<T>(table, startDate));
            list.AddRange(QueryMiddle<T>(table, startDate, endDate));
            list.AddRange(QueryEnd<T>(table, endDate));

            return list;
        }

        /// <summary>
        /// Query the entities which occured on the date here and after the specified time.
        /// </summary>
        private static IEnumerable<T> QueryStart<T>(CloudTable table, DateTime startDate)
            where T : ITableEntity, new()
        {
            var timeOfDayTicks = GetTimeOfDayTicks(startDate);
            var partitionFilter = AzureUtil.GenerateFilterConditionPartitionKey(GetPartitionKey(startDate));
            var ticksFilter = TableQuery.GenerateFilterConditionForLong(
                nameof(CounterEntity.TimeOfDayTicks),
                QueryComparisons.GreaterThanOrEqual,
                timeOfDayTicks);

            var filter = TableQuery.CombineFilters(
                partitionFilter,
                TableOperators.And,
                ticksFilter);

            var query = new TableQuery<T>().Where(filter);
            return table.ExecuteQuery<T>(query);
        }

        private static IEnumerable<T> QueryEnd<T>(CloudTable table, DateTime endDate)
            where T : ITableEntity, new()
        {
            var timeOfDayTicks = GetTimeOfDayTicks(endDate);
            var partitionFilter = AzureUtil.GenerateFilterConditionPartitionKey(GetPartitionKey(endDate));
            var ticksFilter = TableQuery.GenerateFilterConditionForLong(
                nameof(CounterEntity.TimeOfDayTicks),
                QueryComparisons.LessThanOrEqual,
                timeOfDayTicks);

            var filter = TableQuery.CombineFilters(
                partitionFilter,
                TableOperators.And,
                ticksFilter);

            var query = new TableQuery<T>().Where(filter);
            return table.ExecuteQuery<T>(query);
        }

        private static IEnumerable<T> QueryMiddle<T>(CloudTable table, DateTime startDate, DateTime endDate)
            where T : ITableEntity, new()
        {
            var list = new List<T>();
            var max = endDate.Subtract(TimeSpan.FromDays(1));
            for (var cur = startDate.AddDays(1); cur < max; cur = cur.AddDays(1))
            {
                list.AddRange(QueryOne<T>(table, cur));
            }

            return list;
        }

        private static IEnumerable<T> QueryOne<T>(CloudTable table, DateTime date)
            where T : ITableEntity, new()
        {
            var filter = AzureUtil.GenerateFilterConditionPartitionKey(GetPartitionKey(date));
            var query = new TableQuery<T>().Where(filter);
            return table.ExecuteQuery<T>(query);
        }
    }
}
