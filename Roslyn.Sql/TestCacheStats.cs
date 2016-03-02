using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Roslyn.Sql
{
    public class TestCacheStats
    {
        public static readonly TestCacheStats Instance = new TestCacheStats();

        private object _guard = new object();
        private int _missCount;
        private int _hitCount;
        private int _storeCount;
        private List<int> _outputStandardLengthList = new List<int>();
        private List<int> _outputErrorLengthList = new List<int>();
        private List<int> _contentLengthList = new List<int>();

        public TestCacheStatSummary GetCurrentSummary()
        {
            lock (_guard)
            {
                var summary = new TestCacheStatSummary()
                {
                    HitCount = _hitCount,
                    MissCount = _missCount,
                    StoreCount = _storeCount,
                    OutputStandardSummary = TextStatSummary.Create(_outputStandardLengthList),
                    OutputErrorSummary = TextStatSummary.Create(_outputErrorLengthList),
                    ContentSummary = TextStatSummary.Create(_contentLengthList)
                };
                return summary;
            }
        }

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

        public void AddStore(int outputStandardLength, int outputErrorLength, int contentLength)
        {
            lock (_guard)
            {
                _storeCount++;
                _outputStandardLengthList.Add(outputStandardLength);
                _outputErrorLengthList.Add(outputErrorLength);
                _contentLengthList.Add(contentLength);
            }
        }
    }
}