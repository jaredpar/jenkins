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
    public sealed class TestCacheCounterEntity : CounterEntity
    {
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public int StoreCount { get; set; }

        public TestCacheCounterEntity()
        {

        }

        public TestCacheCounterEntity(CounterData counterData) : base(counterData)
        {

        }
    }
}
