using Dashboard.Jenkins;
using System.Collections.Generic;
using System.Linq;

namespace Dashboard.Models
{
    public sealed class WaitingModel
    {
        public int MinimumCount { get; set; }
        public IEnumerable<IGrouping<string, QueuedItemInfo>> Items { get; set; }
    }
}