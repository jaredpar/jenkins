using System.Collections.Generic;

namespace Dashboard.Json
{
    public class DemandBuildModel
    {
        public string UserName { get; set; }

        // DEMAND: Really needs to be commit so that the table storage keys remain unique
        public string BranchOrCommit { get; set; }
        public string RepoUrl { get; set; }
        public List<DemandBuildItem> QueuedItems { get; set; } = new List<DemandBuildItem>();
    }

    public class DemandBuildItem
    {
        public int QueueItemNumber { get; set; }
        public string JobName { get; set; }
    }
}
