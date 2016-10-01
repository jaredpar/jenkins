using Microsoft.WindowsAzure.Storage.Table;

namespace Dashboard.Azure.TestRuns
{
    public sealed class TestRunCounterEntity : TableEntity
    {
        public int RunCount { get; set; }
        public int SucceededCount { get; set; }

        public TestRunCounterEntity()
        {

        }
    }
}
