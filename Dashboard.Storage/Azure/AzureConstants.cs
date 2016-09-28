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
            public const string BuildEvent = "BuildEvent";
            public const string BuildFailureDate = "BuildFailureDate";
            public const string BuildFailureExact = "BuildFailureExact";
            public const string BuildProcessed = "BuildProcessed";
            public const string BuildStateKey = "BuildStateKey";
            public const string BuildState = "BuildState";
            public const string BuildResultDate = "BuildResultDate";
            public const string BuildResultExact = "BuildResultExact";
            public const string TestCacheCounter = "TestCacheCounter";
            public const string TestRunCounter = "TestRunCounter";
            public const string TestRunData = "TestRunData";
            public const string UnitTestQueryCounter = "UnitTestQueryCounter";
            public const string ViewNameDate = "ViewNameDate";

            public static IEnumerable<string> All()
            {
                yield return BuildEvent;
                yield return BuildFailureDate;
                yield return BuildFailureExact;
                yield return BuildState;
                yield return BuildStateKey;
                yield return BuildResultDate;
                yield return BuildResultExact;
                yield return BuildProcessed;
                yield return TestCacheCounter;
                yield return TestRunCounter;
                yield return TestRunData;
                yield return UnitTestQueryCounter;
                yield return ViewNameDate;
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

        public static class QueueNames
        {
            public const string BuildEvent = "build-event";
            public const string ProcessBuild = "process-build";
            public const string EmailBuild = "email-build";

            public static IEnumerable<string> All()
            {
                yield return BuildEvent;
                yield return ProcessBuild;
                yield return EmailBuild;
            }
        }
    }
}
