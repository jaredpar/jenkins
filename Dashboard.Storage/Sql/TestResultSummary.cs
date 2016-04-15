using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Sql
{
    public struct TestResultSummary
    {
        public int Passed { get; }
        public int Failed { get; }
        public int Skipped { get; }
        public TimeSpan Elapsed { get; }

        public TestResultSummary(int passed, int failed, int skipped, TimeSpan elapsed)
        {
            Passed = passed;
            Failed = failed;
            Skipped = skipped;
            Elapsed = elapsed;
        }
    }
}
