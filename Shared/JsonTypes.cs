using System.Collections.Generic;

namespace Dashboard.Json
{
    public class DemandRunRequestModel
    {
        public string UserName { get; set; }
        public string Token { get; set; }
        public string BranchOrCommit { get; set; }
        public string RepoUrl { get; set; }
        public List<string> JobNames { get; set; } = new List<string>();
    }
}
