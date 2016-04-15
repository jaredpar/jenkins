using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Jenkins.Json
{
    public class Build
    {
        public int Number { get; set; }
        public string Url { get; set; }

        public override string ToString() => $"Build {Number}";
    }
}
