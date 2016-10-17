using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure.Builds
{
    public sealed class BuildCounterEntity : TableEntity
    {
        public int CommitSucceededCount { get; set; }
        public int CommitFailedCount { get; set; }
        public int PullRequestSucceededCount { get; set; }
        public int PullRequestFailedCount { get; set; }

        public int CommitBuildCount => CommitSucceededCount + CommitFailedCount;
        public int PullRequestBuildCount => PullRequestFailedCount + PullRequestSucceededCount;

        public BuildCounterEntity()
        {

        }
    }
}
