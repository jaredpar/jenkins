using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    /// <summary>
    /// Entity keeping track of hit / miss counts for <see cref="TestResult"/> instances in the cache.  The rows
    /// are stored in 15 minute chunks. 
    /// </summary>
    public sealed class TestCacheCounterEntity : TableEntity
    {
        private const char s_rowKeySeparatorChar = '!';
        private static readonly char[] s_rowKeySeparatorCharArray = new[] { s_rowKeySeparatorChar };

        public const int MinuteInternal = 15;

        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public int StoreCount { get; set; }
        public bool IsJenkins { get; set; }
        public long TimeOfDayTicks { get; set; }

        /// <summary>
        /// Id representing the source which is updating this particular counter entity.  Allows for 
        /// multiple entities to be updating the same time interval in parallel.  Each is stored as a 
        /// separate entity so there is no write contention.
        /// </summary>
        public string EntityWriterId => SplitRowKey()[0];
        public DateTime Date => DateTime.Parse(PartitionKey);
        public TimeSpan TimeOfDay => TimeSpan.FromTicks(TimeOfDayTicks);
        public EntityKey EntityKey => new EntityKey(PartitionKey, RowKey);

        public TestCacheCounterEntity()
        {

        }

        public static TestCacheCounterEntity Create(DateTime dateTime, string entityWriterId, bool isJenkins)
        {
            Debug.Assert(dateTime.Kind == DateTimeKind.Utc);
            var key = GetEntityKey(dateTime, entityWriterId, isJenkins);
            var entity = new TestCacheCounterEntity()
            {
                PartitionKey = key.PartitionKey,
                RowKey = key.RowKey,
                TimeOfDayTicks = GetTimeOfDayTicks(dateTime),
                IsJenkins = isJenkins,
            };

            return entity;
        }

        public static EntityKey GetEntityKey(DateTime dateTime, string entityWriterId, bool isJenkins)
        {
            return new EntityKey(GetPartitionKey(dateTime), GetRowKey(entityWriterId, dateTime, isJenkins));
        }

        public static string GetPartitionKey(DateTime dateTime)
        {
            Debug.Assert(dateTime.Kind == DateTimeKind.Utc);
            var date = dateTime.Date;
            return date.ToString("yyyy-MM-dd");
        }

        public static string GetRowKey(string entityWriterId, DateTime dateTime, bool isJenkins)
        {
            Debug.Assert(!entityWriterId.Contains(s_rowKeySeparatorChar));
            return $"{entityWriterId}{s_rowKeySeparatorChar}{GetTimeOfDayTicks(dateTime)}{s_rowKeySeparatorChar}{isJenkins}";
        }

        private string[] SplitRowKey()
        {
            return RowKey.Split(s_rowKeySeparatorCharArray, count: 3);
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
