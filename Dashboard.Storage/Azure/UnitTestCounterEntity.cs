using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public class UnitTestCounterEntity : CounterEntity
    {
        public int AssemblyCount { get; set; }
        public int TestsPassed { get; set; }
        public int TestsSkipped { get; set; }
        public int TestsFailed { get; set; }
        public long ElapsedSeconds { get; set; }

        public int TestsTotal => TestsPassed + TestsSkipped + TestsFailed;

        public UnitTestCounterEntity()
        {

        }

        public UnitTestCounterEntity(CounterData counterData) : base(counterData)
        {

        }
    }
}
