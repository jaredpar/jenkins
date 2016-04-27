using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public sealed class JobCounterEntity : CounterEntity
    {
        public const string TableName = AzureConstants.TableNames.JobCounter;


    }
}
