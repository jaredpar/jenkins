using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Sql
{
    public sealed class TestRunLegacy
    {
        public DateTime RunDate { get; }
        public string Cache { get; }
        public TimeSpan Elapsed { get; }
        public bool Succeeded { get; }
        public bool IsJenkins { get; }
        public bool Is32Bit { get; }
        public int AssemblyCount { get; }
        public int CacheCount { get; }
        public int ChunkCount { get; }

        public TestRunLegacy(
            DateTime runDate,
            string cache,
            TimeSpan elapsed,
            bool succeeded,
            bool isJenkins,
            bool is32Bit,
            int cacheCount,
            int assemblyCount,
            int chunkCount)
        {
            RunDate = runDate;
            Cache = cache;
            Elapsed = elapsed;
            Succeeded = succeeded;
            Is32Bit = is32Bit;
            IsJenkins = isJenkins;
            AssemblyCount = assemblyCount;
            CacheCount = cacheCount;
            ChunkCount = chunkCount;
        }
    }
}
