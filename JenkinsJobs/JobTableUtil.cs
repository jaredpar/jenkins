using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JenkinsJobs
{
    internal sealed class JobTableUtil
    {
        private readonly CloudTable _table;

        internal JobTableUtil(CloudTable table)
        {
            _table = table;
        }
    }
}
