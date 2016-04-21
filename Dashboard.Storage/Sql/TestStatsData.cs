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
        public int UploadCount { get; }

        /// <summary>
        /// Count of actively stored <see cref="TestResult"/> values
        /// </summary>
        public int TestResultCount { get; }

        public TestCacheStatSummary(TestHitStats hitStats, int missCount, int uploadCount, int testResultCount)
        {
            HitStats = hitStats;
            MissCount = missCount;
            UploadCount = uploadCount;
            TestResultCount = testResultCount;
        }
    }
}
