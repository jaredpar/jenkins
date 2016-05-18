using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Helpers.Json
{
    public sealed class TestCacheData
    {
        public TestResultData TestResultData { get; set; }
        public TestSourceData TestSourceData { get; set; }
    }

    /// <summary>
    /// The actual test information needs to be cached.
    /// </summary>
    public sealed class TestResultData
    {
        public int ExitCode { get; set; }
        public string OutputStandard { get; set; }
        public string OutputError { get; set; }
        public string ResultsFileName { get; set; }
        public string ResultsFileContent { get; set; }
        public int ElapsedSeconds { get; set; }
        public int TestPassed { get; set; }
        public int TestFailed { get; set; }
        public int TestSkipped { get; set; }

        // Misspelled version to keep until we can flow throw all of the spelling updates.
        public int EllapsedSeconds { get; set; }
    }

    /// <summary>
    /// Extra information about the environment in which the tests were executed.  Helps our
    /// tracking to see how effective the caching is and potentially where errors may be coming
    /// from.
    /// </summary>
    public sealed class TestSourceData
    {
        public string MachineName { get; set; }
        public string EnlistmentRoot { get; set; }
        public string AssemblyName { get; set; }
        public string Source { get; set; }
        public bool IsJenkins { get; set; }
    }


}