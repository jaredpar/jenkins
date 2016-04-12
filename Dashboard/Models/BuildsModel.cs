using Roslyn.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    public class BuildFailureSummary
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class BuildFailureModel
    {
        public string Name { get; set; }
        public List<BuildId> Builds { get; } = new List<BuildId>();
    }
}