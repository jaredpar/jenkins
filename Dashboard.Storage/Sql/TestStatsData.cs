using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Sql
{
    public class TestCacheStatSummary
    {
        public TestHitStats HitStats { get; }
        public int MissCount { get; }
        public int StoreCount { get; }
        public int CacheCount { get; }
        public int RunCount { get; }

        public TestCacheStatSummary(TestHitStats hitStats, int missCount, int storeCount, int cacheCount, int runCount)
        {
            HitStats = hitStats;
            MissCount = missCount;
            StoreCount = storeCount;
            CacheCount = cacheCount;
            RunCount = runCount;
        }
    }
}
