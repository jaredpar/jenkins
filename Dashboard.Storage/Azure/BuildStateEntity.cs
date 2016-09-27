using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    /// <summary>
    /// Represents the state of a build that is tracked.  The key is a <see cref="DateTimeKey"/> based
    /// on date to make for easy querying.
    /// </summary>
    public sealed class BuildStateEntity
    {
        public string HostName { get; set; }
        public int BuildNumber { get; set; }
        public string JobName { get; set; }

        /// <summary>
        /// In the case there was an error processing the build this holds the error text.
        /// </summary>
        public string Error { get; set; }

        public JobId JobId => JobId.ParseName(JobName);
    }
}
