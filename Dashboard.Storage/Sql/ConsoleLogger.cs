using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Sql
{
    public sealed class ConsoleLogger : ILogger
    {
        public void Log(string category, string message)
        {
            Console.WriteLine($"Log {category}: {message}");
        }

        public void Log(string category, string message, Exception ex)
        {
            Log(category, $"{message} {ex}");
        }
    }
}
