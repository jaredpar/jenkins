using System.Collections.Generic;

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
            public const string CounterBuilds = "CounterBuilds";
            public const string CounterTestCache = "CounterTestCache";
            public const string CounterTestCacheJenkins = "CounterTestCacheJenkins";
            public const string CounterUnitTestQuery = "CounterUnitTestQuery";
            public const string CounterUnitTestQueryJenkins = "CounterUnitTestQueryJenkins";
            public const string CounterTestRun = "CounterTestRun";
            public const string CounterTestRunJenkins = "CounterTestRunJenkins";
            public const string TestRunData = "TestRunData";
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
                yield return CounterBuilds;
                yield return CounterTestCache;
                yield return CounterTestCacheJenkins;
                yield return CounterUnitTestQuery;
                yield return CounterUnitTestQueryJenkins;
                yield return CounterTestRun;
                yield return CounterTestRunJenkins;
                yield return TestRunData;
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
