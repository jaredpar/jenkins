using System.Collections.Generic;

namespace Dashboard.Json
{
    public class DemandBuildModel
    {
        public string UserName { get; set; }
        public string Sha1 { get; set; }
        public string RepoUrl { get; set; }
        public List<DemandBuildItem> QueuedItems { get; set; } = new List<DemandBuildItem>();
    }

    public class DemandBuildItem
    {
        public int QueueItemNumber { get; set; }
        public string JobName { get; set; }
    }
}
