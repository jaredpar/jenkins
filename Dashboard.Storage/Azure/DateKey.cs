using System;
using System.Diagnostics;

namespace Dashboard.Azure
{
    /// <summary>
    /// Uses a date as a partition key.  The format of the key is intended to create a 
    /// range partition by using increasing values.
    /// </summary>
    public struct DateKey : IEquatable<DateKey>
    {
        public static readonly DateTimeOffset StartDate = new DateTimeOffset(year: 2015, month: 6, day: 1, hour: 0, minute: 0, second: 0, offset: TimeSpan.FromSeconds(0));

        public DateTimeOffset Date { get; }

        public int Days => (int)(Date - StartDate).TotalDays;
        public string Key => Days.ToString("00000000");

        public DateKey(DateTime date)
        {
            Debug.Assert(date.Kind == DateTimeKind.Utc);
            Debug.Assert(date > StartDate);
            Date = date.Date;
            Debug.Assert(0 == Date.Offset.Ticks);
        }

        public DateKey(DateTimeOffset date)
        {
            Debug.Assert(date > StartDate);
            Date = date.ToUniversalTime();
        }

        public static DateKey Parse(string key)
        {
            var days = TimeSpan.FromDays(int.Parse(key));
            var dateTime = StartDate.Add(days);
            return new DateKey(dateTime);
        }

        public static implicit operator DateKey(DateTimeOffset dateTime) => new DateKey(dateTime);
        public static bool operator==(DateKey left, DateKey right) => left.Date == right.Date;
        public static bool operator!=(DateKey left, DateKey right) => !(left.Date == right.Date);
        public bool Equals(DateKey other) => this == other;
        public override bool Equals(object obj) => obj is DateKey && Equals((DateKey)obj);
        public override int GetHashCode() => Date.GetHashCode();
        public override string ToString() => $"{Date} - {Days}";
    }
}
