using System;

namespace Dashboard.Azure.TestResults
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

        public static BuildSource CreateAnonymous(string suffix = null)
        {
            suffix = suffix ?? $"{Guid.NewGuid().ToString()}";
            return new BuildSource(
                $"anonymous-{suffix}",
                @"c:\anonymous");
        }
    }
}
