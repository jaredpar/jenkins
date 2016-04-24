﻿using System;
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
    }
}
