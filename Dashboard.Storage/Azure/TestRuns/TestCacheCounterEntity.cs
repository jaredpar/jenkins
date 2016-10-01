using Microsoft.WindowsAzure.Storage.Table;

namespace Dashboard.Azure.TestResults
{
    /// <summary>
    /// Entity keeping track of hit / miss counts for <see cref="TestResult"/> instances in the cache.  The rows
    /// are stored in 15 minute chunks. 
    /// </summary>
    public sealed class TestCacheCounterEntity : TableEntity
    {
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public int StoreCount { get; set; }

        public TestCacheCounterEntity()
        {

        }
    }
}
