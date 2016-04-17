using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
