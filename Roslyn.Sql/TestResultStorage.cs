using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Roslyn.Sql
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

    /// <summary>
    /// This is a proof of concept implementation only.  I realize that its implementation is pretty terrible
    /// and that's fine.  For now it is enough to validate the end to end scenario.
    /// </summary>
    public class TestResultStorage : IDisposable
    {
        private const int SizeLimit = 10000000;
        private const int TableRowLimit = 5000;
        private readonly SqlUtil _sqlUtil;

        public List<string> Keys => _sqlUtil.GetTestResultKeys();
        public int Count => _sqlUtil.GetTestResultCount() ?? 0;

        public TestResultStorage(string connectionString)
        {
            _sqlUtil = new SqlUtil(connectionString);
        }

        public void Dispose()
        {
            _sqlUtil.Dispose();
        }

        public void Add(string key, TestResult value)
        {
            if (string.IsNullOrEmpty(value.ResultsFileContent) || value.ResultsFileContent.Length > SizeLimit)
            {
                throw new Exception("Data too big");
            }

            _sqlUtil.InsertTestResult(key, value);

            if ((_sqlUtil.GetTestResultCount() ?? 0) > TableRowLimit)
            {
                _sqlUtil.ShaveTestResultTable();
            }
        }

        public bool TryGetValue(string key, out TestResult testResult)
        {
            var found = _sqlUtil.GetTestResult(key);
            if (found.HasValue)
            {
                testResult = found.Value;
                return true;
            }

            testResult = default(TestResult);
            return false;
        }
    }
}