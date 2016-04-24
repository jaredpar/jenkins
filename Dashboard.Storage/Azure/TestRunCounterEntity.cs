using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public sealed class TestRunCounterEntity : CounterEntity
    {
        public int RunCount { get; set; }
        public int SucceededCount { get; set; }

        public TestRunCounterEntity()
        {

        }

        public TestRunCounterEntity(CounterData data) : base(data)
        {

        }
    }
}
