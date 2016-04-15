using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Sql
{
    public struct TestResult
    {
        public int ExitCode { get; }
        public string OutputStandard { get; }
        public string OutputError { get; }
        public string ResultsFileName { get; }
        public string ResultsFileContent { get; }
        public TimeSpan Elapsed { get; }

        public TestResult(
            int exitCode,
            string outputStandard,
            string outputError,
            string resultsFileName,
            string resultsFileContent,
            TimeSpan elapsed)
        {
            ExitCode = exitCode;
            OutputStandard = outputStandard;
            OutputError = outputError;
            ResultsFileName = resultsFileName;
            ResultsFileContent = resultsFileContent;
            Elapsed = elapsed;
        }
    }
}
