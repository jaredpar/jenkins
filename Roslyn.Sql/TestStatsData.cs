using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Sql
{
    public class TestCacheStatSummary
    {
        public int HitCount { get; }
        public int MissCount { get; }
        public int StoreCount { get; }
        public int CurrentCount { get; }
        public int RunCount { get; }

        public TestCacheStatSummary(int hitCount, int missCount, int storeCount, int currentCount, int runCount)
        {
            HitCount = hitCount;
            MissCount = missCount;
            StoreCount = storeCount;
            CurrentCount = currentCount;
            RunCount = runCount;
        }
    }
}
