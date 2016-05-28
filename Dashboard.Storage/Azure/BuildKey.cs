using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public struct BuildKey
    {
        public BuildId BuildId { get; }

        public string Key => $"{BuildId.Number}-{AzureUtil.NormalizeKey(BuildId.JobName, '_')}";

        public BuildKey(BuildId buildId)
        {
            BuildId = buildId;
        }
    }
}
