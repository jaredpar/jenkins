using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    /// <summary>
    /// There are several methods for storing build result information.  This is the base type containing 
    /// data shared amongst all of the different storage mechanism.
    /// </summary>
    public abstract class BuildEntity : TableEntity
    {
        public string BuildResultKindRaw { get; set; }
        public DateTime BuildDateTime { get; set; }
        public string JobName { get; set; }
        public int BuildNumber { get; set; }
        public string MachineName { get; set; }

        public JobId JobId => JobId.ParseName(JobName);
        public BuildId BuildId => new BuildId(BuildNumber, JobId);
        public BuildResultKind BuildResultKind => (BuildResultKind)Enum.Parse(typeof(BuildResultKind), BuildResultKindRaw);

        protected BuildEntity()
        {

        }

        protected BuildEntity()
        {

        }
    }
}
