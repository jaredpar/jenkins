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
                GetPartitionKey(counterData.DateTime).Key,
                GetRowKey(counterData));
        }

        public static DateKey GetPartitionKey(DateTimeOffset dateTime)
        {
            return new DateKey(dateTime);
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
        public static long GetTimeOfDayTicks(DateTimeOffset dateTime)
        {
            var minute = dateTime.ToUniversalTime().TimeOfDay.Minutes;
            minute = (minute / MinuteInternal) * MinuteInternal;
            var timeOfDay = new TimeSpan(hours: dateTime.TimeOfDay.Hours, minutes: minute, seconds: 0);
            return timeOfDay.Ticks;
        }

        /// <summary>
        /// Query counter entities between the specified dates (inclusive)
        /// </summary>
        public static TableQuery<T> CreateTableQuery<T>(DateTimeOffset startDate, DateTimeOffset endDate)
            where T : CounterEntity, new()
        {
            var startDateKey = new DateKey(startDate);
            var endDateKey = new DateKey(endDate);
            var filter = FilterUtil
                .Combine(
                    FilterUtil.BetweenDateKeys(ColumnNames.PartitionKey, startDateKey, endDateKey),
                    CombineOperator.And,
                    FilterUtil.Combine(
                        FilterUtil.Column(nameof(CounterEntity.DateTimeUtcTicks), startDate.UtcTicks, ColumnOperator.GreaterThanOrEqual),
                        CombineOperator.And,
                        FilterUtil.Column(nameof(CounterEntity.DateTimeUtcTicks), endDate.UtcTicks, ColumnOperator.LessThanOrEqual)));
            return new TableQuery<T>().Where(filter.Filter);
        }

        /// <summary>
        /// Query counter entities between the specified dates (inclusive)
        /// </summary>
        public static IEnumerable<T> Query<T>(CloudTable table, DateTimeOffset startDate, DateTimeOffset endDate)
            where T : CounterEntity, new()
        {
            var query = CreateTableQuery<T>(startDate, endDate);
            return table.ExecuteQuery(query);
        }
    }
}
