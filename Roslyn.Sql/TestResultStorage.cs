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
    public class TestResultStorage
    {
        public static TestResultStorage Instance = new TestResultStorage();

        private const int MapLimit = 5000;
        private const int SizeLimit = 10000000;

        private readonly Dictionary<string, TestResult> _testResultMap = new Dictionary<string, TestResult>();

        public List<string> Keys
        {
            get
            {
                lock (_testResultMap)
                {
                    return _testResultMap.Keys.ToList();
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_testResultMap)
                {
                    return _testResultMap.Count;
                }
            }
        }

        private TestResultStorage()
        {

        }

        public void Add(string key, TestResult value)
        {
            if (string.IsNullOrEmpty(value.ResultsFileContent) || value.ResultsFileContent.Length > SizeLimit)
            {
                throw new Exception("Data too big");
            }

            lock (_testResultMap)
            {
                _testResultMap[key] = value;
                if (_testResultMap.Count > MapLimit)
                {
                    var toRemove = _testResultMap.Keys.Take(MapLimit / 5).ToList();
                    foreach (var item in toRemove)
                    {
                        _testResultMap.Remove(item);
                    }
                }
            }
        }

        public bool TryGetValue(string key, out TestResult testResult)
        {
            lock (_testResultMap)
            {
                return _testResultMap.TryGetValue(key, out testResult);
            }
        }
    }
}