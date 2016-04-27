using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    /// <summary>
    /// Uses a date as a partition key.  The format of the key is intended to create a 
    /// range partition by using increasing values.
    /// </summary>
    public struct DatePartitionKey : IEquatable<DatePartitionKey>
    {
        public static readonly DateTime StartDate = AzureUtil.DefaultStartDate.ToUniversalTime();

        public DateTime Date { get; }

        public int Days => (int)(Date - StartDate).TotalDays;
        public string PartitionKey => Days.ToString("00000000");

        public DatePartitionKey(DateTime date)
        {
            Debug.Assert(date.Kind == DateTimeKind.Utc);
            Debug.Assert(date > StartDate);
            Date = date.Date;
        }

        public static DatePartitionKey ParsePartitionKey(string key) => new DatePartitionKey(StartDate.Add(TimeSpan.FromDays(int.Parse(key))));
        public static bool operator==(DatePartitionKey left, DatePartitionKey right) => left.Date == right.Date;
        public static bool operator!=(DatePartitionKey left, DatePartitionKey right) => !(left.Date == right.Date);
        public bool Equals(DatePartitionKey other) => this == other;
        public override bool Equals(object obj) => obj is DatePartitionKey && Equals((DatePartitionKey)obj);
        public override int GetHashCode() => Date.GetHashCode();
        public override string ToString() => $"{Date} - {Days}";
    }
}
