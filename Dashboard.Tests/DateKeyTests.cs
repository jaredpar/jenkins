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
    public class DateKeyTests
    {
        [Fact]
        public void Equality()
        {
            var date = new DateTime(year: 2016, month: 4, day: 1).ToUniversalTime();
            EqualityUnit
                .Create(new DateKey(date))
                .WithEqualValues(new DateKey(date))
                .WithNotEqualValues(new DateKey(new DateTime(year: 2016, month: 4, day: 2).ToUniversalTime()))
                .RunAll(
                    (x, y) => x == y,
                    (x, y) => x != y);
        }

        [Fact]
        public void PartitionKey()
        {
            var key1 = new DateKey(new DateTime(year: 2016, month: 4, day: 1).ToUniversalTime());
            var key2 = new DateKey(new DateTime(year: 2016, month: 4, day: 2).ToUniversalTime());
            Assert.Equal("00000305", key1.Key);
            Assert.Equal("00000306", key2.Key);
        }

        [Fact]
        public void ParsePartitionKey()
        {
            var key1 = new DateKey(new DateTime(year: 2016, month: 4, day: 1).ToUniversalTime());
            var key2 = DateKey.Parse(key1.Key);
            Assert.Equal(key1, key2);
        }
    }
}
