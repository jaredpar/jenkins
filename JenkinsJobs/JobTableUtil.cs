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

        // TODO: Need a Jenkins token as well to be able to query our non-public jobs.
        internal JobTableUtil(CloudTable table)
        {
            _table = table;
        }

        internal void Populate()
        {

        }
    }
}
