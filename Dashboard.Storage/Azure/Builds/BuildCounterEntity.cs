using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure.Builds
{
    public sealed class BuildCounterEntity : TableEntity
    {
        public int BuildCount { get; set; }
        public int SuccededCount { get; set; }
        public int FailedCount { get; set; }

        public BuildCounterEntity()
        {

        }
    }
}
