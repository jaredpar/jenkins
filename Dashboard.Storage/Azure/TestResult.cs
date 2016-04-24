using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public struct TestResult
    {
        public int ExitCode { get; }
        public string OutputStandard { get; }
        public string OutputError { get; }
        public string ResultsFileName { get; }
        public string ResultsFileContent { get; }
        public UnitTestData UnitTestData { get; }
        public TimeSpan Elapsed { get; }

        public TestResult(
            int exitCode,
            string outputStandard,
            string outputError,
            string resultsFileName,
            string resultsFileContent,
            UnitTestData unitTestData,
            TimeSpan elapsed)
        {
            ExitCode = exitCode;
            OutputStandard = outputStandard;
            OutputError = outputError;
            ResultsFileName = resultsFileName;
            ResultsFileContent = resultsFileContent;
            UnitTestData = unitTestData;
            Elapsed = elapsed;
        }
    }

    public struct UnitTestData
    {
        public int Passed { get; }
        public int Failed { get; }
        public int Skipped { get; }

        public int Total => Passed + Failed + Skipped;

        public UnitTestData(int passed, int failed, int skipped)
        {
            Passed = passed;
            Failed = failed;
            Skipped = skipped;
        }

        public override string ToString() => $"Passed: {Passed} Failed: {Failed} Skipped: {Skipped}";
    }
}
