using Dashboard.Jenkins;
using System;

namespace Dashboard.StorageBuilder
{
    public struct BuildAnalyzeError
    {
        public BuildId BuildId { get; }
        public Exception Exception { get; }

        public BuildAnalyzeError(BuildId id, Exception ex)
        {
            BuildId = id;
            Exception = ex;
        }
    }
}
