using System;
using System.Globalization;
using System.Text;

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

        public DateTimeOffset DateTime => ParseDateTime(Key, Flags);

        public DateTimeKey(DateTimeOffset dateTime, DateTimeKeyFlags flags)
        {
            Key = GetKey(dateTime, flags);
            Flags = flags;
        }

        public static string GetKey(DateTimeOffset dateTime, DateTimeKeyFlags flags)
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

        public static DateTimeKey ParseDateTimeKey(string key, DateTimeKeyFlags flags)
        {
            var dateTime = ParseDateTime(key, flags);
            return new DateTimeKey(dateTime, flags);
        }

        public static bool TryParseDateTimeKey(string key, DateTimeKeyFlags flags, DateTimeKey dateTimeKey)
        {
            DateTimeOffset dateTime;
            if (!TryParseDateTime(key, flags, out dateTime))
            {
                return false;
            }

            dateTimeKey = new DateTimeKey(dateTime, flags);
            return true;
        }

        public static DateTimeOffset ParseDateTime(string key, DateTimeKeyFlags flags)
        {
            DateTimeOffset dateTime;
            if (!TryParseDateTime(key, flags, out dateTime))
            {
                throw new Exception($"Unable to parse key: {key}");
            }

            return dateTime;
        }

        public static bool TryParseDateTime(string key, DateTimeKeyFlags flags, out DateTimeOffset dateTime)
        {
            var hours = DateTimeKeyFlags.Hours == (flags & DateTimeKeyFlags.Hours);
            var minutes = DateTimeKeyFlags.Minutes == (flags & DateTimeKeyFlags.Minutes);

            var provider = CultureInfo.InvariantCulture;
            if (flags == DateTimeKeyFlags.Date)
            {
                return DateTimeOffset.TryParseExact(key, "yyyyMMdd", provider, DateTimeStyles.AdjustToUniversal, out dateTime);
            }
            else
            {
                return DateTimeOffset.TryParseExact(key, @"yyyyMMdd\THHmm", provider, DateTimeStyles.AdjustToUniversal, out dateTime);
            }
        }

        public static bool operator ==(DateTimeKey left, DateTimeKey right) => left.Key == right.Key;
        public static bool operator !=(DateTimeKey left, DateTimeKey right) => !(left == right);
        public bool Equals(DateTimeKey other) => this == other;
        public override bool Equals(object obj) => obj is DateTimeKey && Equals((DateTimeKey)obj);
        public override int GetHashCode() => Key.GetHashCode();
        public override string ToString() => Key;
    }
}
