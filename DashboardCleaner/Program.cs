using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Configuration;
using Roslyn.Sql;
using SendGrid;
using System.Net.Mail;
using Roslyn;

namespace DashboardCleaner
{
    internal static class Program
    {
        public static void Main()
        {
            var testResultDeletedRowCount = CleanTestResultTable();
            SendEmail(testResultDeletedRowCount).Wait();

            /*
            var host = new JobHost();
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
            */
        }

        private static async Task SendEmail(int testResultDeletedRowCount)
        {
            var message = new SendGridMessage();
            message.AddTo("jaredpparsons@gmail.com");
            message.AddTo("jaredpar@microsoft.com");
            message.From = new MailAddress("jaredpar@jdash.azurewebsites.net");
            message.Subject = "Dashboard cleaner summary";
            message.Text = $"Deleted {testResultDeletedRowCount} rows from TestResult";

            try
            {
                var key = ConfigurationManager.AppSettings["sendgrid-api-key"];
                var web = new Web(apiKey: key);
                await web.DeliverAsync(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to send message: {ex}");
            }
        }

        private static int CleanTestResultTable()
        { 
            Console.WriteLine("Cleaning TestResult Table");

            var total = 0;
            var connectionString = ConfigurationManager.AppSettings[SharedConstants.SqlConnectionStringName];
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
                        total += count.Value;
                    }
                    else
                    {
                        Console.WriteLine("Done cleaning");
                        done = true;
                    }
                } while (!done);
            }

            Console.WriteLine($"Removed {total} rows");

            return total;
        }
    }
}
