using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    public sealed class WaitingModel
    {
        public int MinimumCount { get; set; }
        public IEnumerable<IGrouping<string, QueuedItemInfo>> Items { get; set; }
    }
}