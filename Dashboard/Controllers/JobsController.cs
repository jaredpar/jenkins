using Dashboard.Models;
using Roslyn.Jenkins;
using Roslyn.Sql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Dashboard.Controllers
{
    public class JobsController : Controller
    {
        public ActionResult Index()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Jenkins"].ConnectionString;
            using (var client = new DataClient(connectionString))
            {
                var model = new AllJobsModel();
                model.Names.AddRange(client.GetJobNames());
                return View(model);
            }
        }

        public ActionResult Name(string name = null)
        {
            name = name ?? "roslyn_master_win_dbg_unit32";

            var connectionString = ConfigurationManager.ConnectionStrings["Jenkins"].ConnectionString;
            using (var client = new DataClient(connectionString))
            {
                var duration = client.GetAverageDuration(name);
                var model = new JobModel()
                {
                    Name = name,
                    AverageDuration = duration
                };
                model.DailyAverageDuration.AddRange(client.GetDailyAverageDurations(name));
                model.JobDaySummaryList.AddRange(GetDailySummary(client, name));

                return View(model);
            }
        }

        private IEnumerable<JobDaySummary> GetDailySummary(DataClient client, string jobName)
        {
            var map = new Dictionary<DateTime, JobDaySummary>();
            Action<DateTime, Action<JobDaySummary>> update = (date, callback) =>
            {
                date = date.Date;
                JobDaySummary summary;
                if (!map.TryGetValue(date, out summary))
                {
                    summary = new JobDaySummary();
                    summary.Date = date;
                    map[date] = summary;
                }

                callback(summary);
            };

            client.GetDailyJobCount(jobName, JobState.Succeeded).ForEach(x => update(x.Item1, y => y.Succeeded = x.Item2));
            client.GetDailyJobCount(jobName, JobState.Failed).ForEach(x => update(x.Item1, y => y.Failed = x.Item2));
            client.GetDailyJobCount(jobName, JobState.Aborted).ForEach(x => update(x.Item1, y => y.Aborted = x.Item2));
            return map
                .OrderBy(pair => pair.Key)
                .Select(pair => pair.Value);
        }
    }
}