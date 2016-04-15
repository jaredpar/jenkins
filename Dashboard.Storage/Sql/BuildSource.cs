using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Sql
{
    public struct BuildSource
    {
        public string MachineName { get; }
        public string EnlistmentRoot { get; }

        public BuildSource(string machineName, string enlistmentRoot)
        {
            MachineName = machineName;
            EnlistmentRoot = enlistmentRoot;
        }
    }
}
