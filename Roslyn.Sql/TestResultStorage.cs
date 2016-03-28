using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Roslyn.Sql
{
    /// <summary>
    /// This is a proof of concept implementation only.  I realize that its implementation is pretty terrible
    /// and that's fine.  For now it is enough to validate the end to end scenario.
    /// </summary>
    public class TestResultStorage
    {
        private const int SizeLimit = 10000000;
        private readonly SqlUtil _sqlUtil;

        public List<string> Keys => _sqlUtil.GetTestResultKeys();
        public int Count => _sqlUtil.GetTestResultCount() ?? 0;

        public TestResultStorage(SqlUtil sqlUtil)
        {
            _sqlUtil = sqlUtil;
        }

        public void Add(string key, TestResult value)
        {
            if (string.IsNullOrEmpty(value.ResultsFileContent) || value.ResultsFileContent.Length > SizeLimit)
            {
                throw new Exception("Data too big");
            }

            _sqlUtil.InsertTestResult(key, value);
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