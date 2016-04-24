using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
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

        public static BuildSource CreateAnonymous()
        {
            return new BuildSource(
                $"anonymous-{DateTime.UtcNow.Ticks}",
                @"c:\anonymous");
        }
    }
}
