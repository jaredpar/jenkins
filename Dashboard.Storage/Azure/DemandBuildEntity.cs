using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public enum DemandBuildStatus
    {
        Queued,
        Running,
        Completed
    }

    public class DemandBuildEntity : TableEntity
    {
        public int QueueItemNumber { get; set; }

        /// <summary>
        /// This value is not valid until the entity is in at least the Running state.  Before that it will 
        /// have the numeric value 0.
        /// </summary>
        public int BuildNumber { get; set; }
        public string StatusRaw { get; set; }
        public string JobName { get; set; }
        public string BuildResult { get; set; }

        public string Sha1 => RowKey;
        public DemandBuildStatus Status => (DemandBuildStatus)Enum.Parse(typeof(DemandBuildStatus), StatusRaw);

        public DemandBuildEntity()
        {

        }

        public DemandBuildEntity(string userName, string sha1, int queueItemNumber, string jobName) : base(userName, sha1)
        {
            QueueItemNumber = queueItemNumber;
            JobName = jobName;
            StatusRaw = DemandBuildStatus.Queued.ToString();
        }
    }
}
