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
    /// Entity keeping track of hit / miss counts for <see cref="TestResult"/> instances.  The rows
    /// are stored in 15 minute chunks. 
    /// </summary>
    public sealed class TestResultCounterEntity : TableEntity
    {
        public const int MinuteInternal = 15;

        public int NormalHitCount { get; set; }
        public int NormalMissCount { get; set; }
        public int JenkinsHitCount { get; set; }
        public int JenkinsMissCount { get; set; }
        public int StoreCount { get; set; }
        public int RunCount { get; set; }
        public long TimeOfDayTicks { get; set; }

        /// <summary>
        /// Id representing the source which is updating this particular counter entity.  Allows for 
        /// multiple entities to be updating the same time interval in parallel.  Each is stored as a 
        /// separate entity so there is no write contention.
        /// </summary>
        public string EntityWriterId => RowKey;
        public DateTime Date => DateTime.Parse(PartitionKey);
        public TimeSpan TimeOfDay => TimeSpan.FromTicks(TimeOfDayTicks);

        public TestResultCounterEntity()
        {

        }

        public static TestResultCounterEntity Create(DateTime dateTime, string entityWriterId)
        {
            Debug.Assert(dateTime.Kind == DateTimeKind.Utc);
            var key = GetEntityKey(dateTime, entityWriterId);
            var entity = new TestResultCounterEntity()
            {
                PartitionKey = key.PartitionKey,
                RowKey = key.RowKey,
                TimeOfDayTicks = GetTimeOfDayTicks(dateTime)
            };

            return entity;
        }

        public static EntityKey GetEntityKey(DateTime dateTime, string entityWriterId)
        {
            Debug.Assert(dateTime.Kind == DateTimeKind.Utc);
            var date = dateTime.Date;
            return new EntityKey(date.ToString(), entityWriterId);
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
