using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Configuration;
using Roslyn.Sql;

namespace DashboardCleaner
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    public class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        public  static void Main()
        {
            CleanTestResultTable();

            /*
            var host = new JobHost();
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
            */
        }

        private static void CleanTestResultTable()
        {
            Console.WriteLine("Cleaning TestResult Table");

            var connectionString = ConfigurationManager.AppSettings["jenkins-connection-string"];
            using (var sqlUtil = new SqlUtil(connectionString, new ConsoleLogger()))
            {
                var date = DateTime.UtcNow - TimeSpan.FromDays(14);
                var done = false;
                do
                {
                    var count = sqlUtil.CleanTestResultTable(count: 100, storeDate: date);
                    if (count > 0)
                    {
                        Console.WriteLine($"Cleaned {count}");
                    }
                    else
                    {
                        Console.WriteLine("Done cleaning");
                        done = true;
                    }
                } while (!done);
            }
        }
    }
}
