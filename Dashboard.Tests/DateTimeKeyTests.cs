using Dashboard.Azure;
using Dashboard.Jenkins.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dashboard.Tests
{
    public class DateTimeKeyTests
    {
        public sealed class EqualityTests : DateTimeKeyTests
        {
            [Fact]
            public void Date()
            {
                var date = new DateTimeOffset(year: 2016, month: 4, day: 1, hour: 1, minute: 0, second: 0, offset: TimeSpan.Zero);
                EqualityUnit
                    .Create(new DateTimeKey(date, DateTimeKeyFlags.Date))
                    .WithEqualValues(
                        new DateTimeKey(date, DateTimeKeyFlags.Date),
                        new DateTimeKey(date.AddHours(1), DateTimeKeyFlags.Date))
                    .WithNotEqualValues(
                        new DateTimeKey(date, DateTimeKeyFlags.Time),
                        new DateTimeKey(date.AddDays(1), DateTimeKeyFlags.Date))
                    .RunAll(
                        (x, y) => x == y,
                        (x, y) => x != y);
            }

            [Fact]
            public void Time()
            {
                var date = new DateTimeOffset(year: 2016, month: 4, day: 1, hour: 1, minute: 0, second: 0, offset: TimeSpan.Zero);
                EqualityUnit
                    .Create(new DateTimeKey(date, DateTimeKeyFlags.Time))
                    .WithEqualValues(
                        new DateTimeKey(date, DateTimeKeyFlags.Time),
                        new DateTimeKey(date.AddDays(1), DateTimeKeyFlags.Time))
                    .WithNotEqualValues(
                        new DateTimeKey(date, DateTimeKeyFlags.Date),
                        new DateTimeKey(date.AddHours(1), DateTimeKeyFlags.Time))
                    .RunAll(
                        (x, y) => x == y,
                        (x, y) => x != y);
            }
        }

        public sealed class KeyTests
        {
            [Fact]
            public void DateSimple()
            {
                var date = DateTimeOffset.Parse("2016/09/15");
                var key = new DateTimeKey(date, DateTimeKeyFlags.Date);
                Assert.Equal("20160915", key.Key);
            }

            [Fact]
            public void DateTimeSimple()
            {
                var date = DateTimeOffset.Parse("2016/09/15 1:00AM");
                var key = new DateTimeKey(date, DateTimeKeyFlags.DateTime);
                Assert.Equal("20160915T0100", key.Key);
            }

            [Fact]
            public void DateTimeWidth()
            {
                var date = DateTimeOffset.Parse("2016/1/2 1:01PM");
                var key = new DateTimeKey(date, DateTimeKeyFlags.DateTime);
                Assert.Equal("20160102T1301", key.Key);
            }

            [Fact]
            public void DateTimeMilitary()
            {
                var date = DateTimeOffset.Parse("2016/09/15 1:00PM");
                var key = new DateTimeKey(date, DateTimeKeyFlags.DateTime);
                Assert.Equal("20160915T1300", key.Key);
            }

            [Fact]
            public void DateComparisonYears()
            {
                var date = DateTimeOffset.Parse("2016/09/15 1:00PM");
                var key = new DateTimeKey(date, DateTimeKeyFlags.Date);
                for (var i = 1; i < 1000; i++)
                {
                    var newKey = new DateTimeKey(date.AddYears(i), key.Flags);
                    Assert.True(string.CompareOrdinal(newKey.Key, key.Key) > 0);
                }
            }

            [Fact]
            public void DateComparisonDays()
            {
                var date = DateTimeOffset.Parse("2016/09/15 1:00PM");
                var key = new DateTimeKey(date, DateTimeKeyFlags.Date);
                for (var i = 1; i < 1000; i++)
                {
                    var newKey = new DateTimeKey(date.AddDays(i), key.Flags);
                    Assert.True(string.CompareOrdinal(newKey.Key, key.Key) > 0);
                }
            }

            [Fact]
            public void DateTimeComparisonHours()
            {
                var date = DateTimeOffset.Parse("2016/09/15 1:00PM");
                var key = new DateTimeKey(date, DateTimeKeyFlags.DateTime);
                for (var i = 1; i < 100; i++)
                {
                    var newKey = new DateTimeKey(date.AddHours(i), key.Flags);
                    Assert.True(string.CompareOrdinal(newKey.Key, key.Key) > 0);
                }
            }

            [Fact]
            public void DateTimeComparisonMinutes()
            {
                var date = DateTimeOffset.Parse("2016/09/15 1:00PM");
                var key = new DateTimeKey(date, DateTimeKeyFlags.DateTime);
                for (var i = 1; i < 100; i++)
                {
                    var newKey = new DateTimeKey(date.AddMinutes(i), key.Flags);
                    Assert.True(string.CompareOrdinal(newKey.Key, key.Key) > 0);
                }
            }
        }
    }
}
