using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Roslyn.Sql
{
    public class TestCacheStats : IDisposable
    {
        private readonly SqlUtil _sqlUtil;

        public TestCacheStats(string connectionString = null)
        {
            _sqlUtil = new SqlUtil(connectionString);
        }

        public void Dispose()
        {
            _sqlUtil.Dispose();
        }

        public TestCacheStatSummary GetCurrentSummary()
        {
            var tuple = _sqlUtil.GetStats();
            return new TestCacheStatSummary(
                hitCount: tuple.Item1,
                missCount: tuple.Item2,
                storeCount: _sqlUtil.GetStoreCount() ?? 0,
                currentCount: TestResultStorage.Instance.Count);
        }

        public void AddHit(string checksum, string assemblyName, bool? isJenkins)
        {
            _sqlUtil.InsertHit(checksum, assemblyName, isJenkins);
        }

        public void AddMiss(string checksum, string assemblyName, bool? isJenkins)
        {
            _sqlUtil.InsertMiss(checksum, assemblyName, isJenkins);
        }

        public void AddStore(string checksum, string assemblyName, int outputStandardLength, int outputErrorLength, int contentLength, TimeSpan ellapsed)
        {
            _sqlUtil.Insert(checksum, assemblyName, outputStandardLength, outputErrorLength, contentLength, ellapsed);
        }
    }
}