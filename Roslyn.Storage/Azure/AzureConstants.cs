using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Azure
{
    public static class AzureConstants
    {
        public const string StorageConnectionStringName = "jaredpar-storage-connectionstring";

        public const string TableNameBuildFailure = "BuildFailure";
        public const string TableNameBuildProcessed = "BuildProcessed";
    }
}
