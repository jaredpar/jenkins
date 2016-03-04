using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Sql
{
    internal interface ILogger
    {
        void Log(string category, string message);
        void Log(string category, string message, Exception ex);
    }
}
