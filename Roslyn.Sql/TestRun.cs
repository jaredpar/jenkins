using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Sql
{
    public sealed class TestRun
    {
        public DateTime RunDate { get; }
        public string Cache { get; }
        public TimeSpan Ellapsed { get; }
        public bool IsJenkins { get; }
        public bool Is32Bit { get; }
        public int AssemblyCount { get; }
        public int CacheCount { get; }

        public TestRun(
            DateTime runDate,
            string cache,
            TimeSpan ellapsed,
            bool isJenkins,
            bool is32Bit,
            int cacheCount,
            int assemblyCount)
        {
            RunDate = runDate;
            Cache = cache;
            Ellapsed = ellapsed;
            Is32Bit = is32Bit;
            IsJenkins = isJenkins;
            AssemblyCount = assemblyCount;
            CacheCount = cacheCount;
        }
    }
}
