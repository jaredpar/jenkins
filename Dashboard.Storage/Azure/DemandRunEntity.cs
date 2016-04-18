using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public enum DemandRunStatus
    {
        Running,
        Completed
    }

    /// <summary>
    /// Stores the information for a demand jenkins run of a given sha1.
    /// </summary>
    public class DemandRunEntity : TableEntity
    {
        public string StatusRaw { get; set; }
        public string RepoUrlRaw { get; set; }

        public Uri RepoUrl => new Uri(RepoUrlRaw);
        public DemandRunStatus Status => (DemandRunStatus)Enum.Parse(typeof(DemandRunStatus), StatusRaw);
        public string Commit => RowKey;

        public DemandRunEntity()
        {

        }

        public DemandRunEntity(string userName, string commit, Uri repoUrl) : base(partitionKey: userName, rowKey: commit)
        {
            StatusRaw = DemandRunStatus.Running.ToString();
            RepoUrlRaw = repoUrl.ToString();
        }
    }
}
