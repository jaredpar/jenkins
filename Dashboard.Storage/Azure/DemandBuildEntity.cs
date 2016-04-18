using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public class DemandBuildEntity : TableEntity
    {
        public int QueueItemNumber { get; set; }
        public int BuildNumber { get; set; }
        public string JobName { get; set; }

        public string Commit => RowKey;
        public JobId JobId => JobId.ParseName(JobName);
        public BuildId BuildId => new BuildId(BuildNumber, JobId);

        public DemandBuildEntity()
        {

        }

        public DemandBuildEntity(string userName, string commit, int queueItemNumber, string jobName, int buildNumber) : base(userName, commit)
        {
            QueueItemNumber = queueItemNumber;
            JobName = jobName;
            BuildNumber = buildNumber;
        }
    }
}
