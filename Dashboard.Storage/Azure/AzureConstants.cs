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
            public const string DemandRun = "DemandRun";
            public const string DemandBuild = "DemandBuild";
            public const string TestCacheCounterTable = "TestCacheCounter";
        }

        public static class ContainerNames
        {
            public const string TestResults = "testresults";
        }

        public static class BlobDirectoryNames
        {
            public const string TestResults = "testResults";
        }
    }
}
