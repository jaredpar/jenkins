using Microsoft.WindowsAzure.Storage.Table;

namespace Dashboard.Azure.TestResults
{
    public class UnitTestCounterEntity : TableEntity
    {
        public int AssemblyCount { get; set; }
        public int TestsPassed { get; set; }
        public int TestsSkipped { get; set; }
        public int TestsFailed { get; set; }
        public long ElapsedSeconds { get; set; }

        public int ClientHitCount => AssemblyCount;
        public int TestsTotal => TestsPassed + TestsSkipped + TestsFailed;

        public UnitTestCounterEntity()
        {

        }
    }
}
