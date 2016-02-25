using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Helpers
{
    public class TestCacheStats
    {
        public static readonly TestCacheStats Instance = new TestCacheStats();

        private object _guard = new object();
        private int _missCount;
        private int _hitCount;
        private int _storeCount;

        public int MissCount => _missCount;
        public int HitCount => _hitCount;
        public int StoreCount => _storeCount;

        public void AddHit()
        {
            lock (_guard)
            {
                _hitCount++;
            }
        }

        public void AddMiss()
        {
            lock (_guard)
            {
                _missCount++;
            }
        }

        public void AddStore()
        {
            lock (_guard)
            {
                _storeCount++;
            }
        }
    }
}