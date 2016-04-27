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
    public class DatePartitionKeyTests
    {
        [Fact]
        public void Equality()
        {
            var date = new DateTime(year: 2016, month: 4, day: 1).ToUniversalTime();
            EqualityUnit
                .Create(new DatePartitionKey(date))
                .WithEqualValues(new DatePartitionKey(date))
                .WithNotEqualValues(new DatePartitionKey(new DateTime(year: 2016, month: 4, day: 2).ToUniversalTime()))
                .RunAll(
                    (x, y) => x == y,
                    (x, y) => x != y);
        }

        [Fact]
        public void PartitionKey()
        {
            var key1 = new DatePartitionKey(new DateTime(year: 2016, month: 4, day: 1).ToUniversalTime());
            var key2 = new DatePartitionKey(new DateTime(year: 2016, month: 4, day: 2).ToUniversalTime());
            Assert.Equal("00000030", key1.PartitionKey);
            Assert.Equal("00000031", key2.PartitionKey);
        }

        [Fact]
        public void ParsePartitionKey()
        {
            var key1 = new DatePartitionKey(new DateTime(year: 2016, month: 4, day: 1).ToUniversalTime());
            var key2 = DatePartitionKey.ParsePartitionKey(key1.PartitionKey);
            Assert.Equal(key1, key2);
        }
    }
}
