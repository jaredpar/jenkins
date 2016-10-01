using Microsoft.WindowsAzure.Storage.Table;

namespace Dashboard.Azure.TestResults
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
