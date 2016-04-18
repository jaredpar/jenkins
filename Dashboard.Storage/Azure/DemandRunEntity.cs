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
        Created,
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
        public string Sha1 => RowKey;

        public DemandRunEntity()
        {

        }

        public DemandRunEntity(string userName, string sha1, Uri repoUrl) : base(partitionKey: userName, rowKey: sha1)
        {
            StatusRaw = DemandRunStatus.Created.ToString();
            RepoUrlRaw = repoUrl.ToString();
        }
    }
}
