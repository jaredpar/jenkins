using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public static class AzureConstants
    {
        public static class TableNames
        {
            public const string BuildFailure = "BuildFailure";
            public const string BuildProcessed = "BuildProcessed";
            public const string BuildEvent = "BuildEvent";
            public const string DemandRun = "DemandRun";
            public const string DemandBuild = "DemandBuild";
            public const string TestCacheCounter = "TestCacheCounter";
            public const string TestRunCounter = "TestRunCounter";
            public const string TestRunData = "TestRunData";
            public const string UnitTestQueryCounter = "UnitTestQueryCounter";

            public static IEnumerable<string> All()
            {
                yield return BuildFailure;
                yield return BuildProcessed;
                yield return BuildEvent;
                yield return DemandRun;
                yield return DemandBuild;
                yield return TestCacheCounter;
                yield return TestRunCounter;
                yield return UnitTestQueryCounter;
                yield return TestRunData;
            }
        }

        public static class ContainerNames
        {
            public const string TestResults = "testresults";

            public static IEnumerable<string> All()
            {
                yield return TestResults;
            }
        }

        public static class BlobDirectoryNames
        {
            public const string TestResults = "testResults";
        }

        public static class QueueNames
        {
            public const string BuildEvent = "build-event";

            public static IEnumerable<string> All()
            {
                yield return BuildEvent;
            }
        }
    }
}
