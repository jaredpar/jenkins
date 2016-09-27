using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    [Flags]
    public enum DateTimeKeyFlags
    {
        Default = Date,
        DateTime = Date | Time,
        Time = Minutes | Hours,

        Date = 0x001,
        Hours = 0x002,
        Minutes = 0x004,
    }

    /// <summary>
    /// Uses a date as a partition key.  The format of the key is intended to create a 
    /// range partition by using increasing values.
    /// </summary>
    public struct DateTimeKey : IEquatable<DateTimeKey>
    {
        public string Key { get; }
        public DateTimeKeyFlags Flags { get; }

        public DateTimeKey(DateTimeOffset dateTime, DateTimeKeyFlags flags)
        {
            Key = CreateKey(dateTime, flags);
            Flags = flags;
        }

        public static string CreateKey(DateTimeOffset dateTime, DateTimeKeyFlags flags)
        {
            var builder = new StringBuilder();
            if (DateTimeKeyFlags.Date == (flags & DateTimeKeyFlags.Date))
            {
                builder.Append(dateTime.Year.ToString("0000"));
                builder.Append(dateTime.Month.ToString("00"));
                builder.Append(dateTime.Day.ToString("00"));
            }

            var hours = DateTimeKeyFlags.Hours == (flags & DateTimeKeyFlags.Hours);
            var minutes = DateTimeKeyFlags.Minutes == (flags & DateTimeKeyFlags.Minutes);
            if (hours || minutes)
            {
                builder.Append("T");
                builder.Append(hours ? dateTime.Hour.ToString("00") : "00");
                builder.Append(minutes ? dateTime.Minute.ToString("00") : "00");
            }

            return builder.ToString();
        }

        public static bool operator==(DateTimeKey left, DateTimeKey right) => left.Key == right.Key;
        public static bool operator!=(DateTimeKey left, DateTimeKey right) => !(left == right);
        public bool Equals(DateTimeKey other) => this == other;
        public override bool Equals(object obj) => obj is DateTimeKey && Equals((DateTimeKey)obj);
        public override int GetHashCode() => Key.GetHashCode();
        public override string ToString() => Key;
    }
}
