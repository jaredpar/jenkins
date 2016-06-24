using Dashboard.Models;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Dashboard;
using Dashboard.Azure;
using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Threading.Tasks;
using Dashboard.Json;

namespace Dashboard.Controllers
{
    public class BuildsApiController : ApiController
    {
        private readonly DashboardStorage _storage;
        private readonly BuildUtil _buildUtil;

        public BuildsApiController()
        {
            var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            _storage = new DashboardStorage(connectionString);
            _buildUtil = new BuildUtil(_storage.StorageAccount);
        }

        /// <summary>
        /// Get all of the the test based failures since the provided date.  Default is 1 day. 
        /// </summary>
        [Route("api/builds/testFailures")]
        public List<TestFailureData> GetTestFailures([FromUri] DateTimeOffset? startDate = null, [FromUri] string viewName = AzureUtil.ViewNameAll)
        {
            var startDateValue = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            var list = new List<TestFailureData>();
            foreach (var group in _buildUtil.GetTestCaseFailures(startDateValue, viewName).GroupBy(x => x.Identifier))
            {
                var commitFailure = 0;
                var prFailure = 0;
                foreach (var entity in group)
                {
                    if (JobUtil.IsPullRequestJobName(entity.BuildId.JobName))
                    {
                        prFailure++;
                    }
                    else
                    {
                        commitFailure++;
                    }
                }

                var item = new TestFailureData()
                {
                    Name = group.Key,
                    TotalFailures = commitFailure + prFailure,
                    CommitFailures = commitFailure,
                    PullRequestFailures = prFailure
                };

                list.Add(item);
            }

            return list;
        }

        [Route("api/builds/testFailure")]
        public TestFailureData GetTestFailure([FromUri] string name, [FromUri] DateTimeOffset? startDate = null, string viewName = AzureUtil.ViewNameRoslyn)
        {
            var data = new TestFailureData()
            {
                Name = name,
                Builds = new List<BuildData>()
            };

            var startDateValue = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            var prCount = 0;
            var commitCount = 0;
            foreach (var entity in _buildUtil.GetTestCaseFailures(startDateValue, name, viewName))
            {
                var buildId = entity.BuildId;
                var jobId = buildId.JobId;
                if (JobUtil.IsPullRequestJobName(jobId.Name))
                {
                    prCount++;
                }
                else
                {
                    commitCount++;
                }

                var buildData = new BuildData()
                {
                    JobName = jobId.Name,
                    JobShortName = jobId.ShortName,
                    JobUri = JenkinsUtil.GetUri(SharedConstants.DotnetJenkinsUri, jobId).ToString(),
                    MachineName = entity.MachineName,
                    DateTime = startDateValue,
                };

                data.Builds.Add(buildData);
            }

            data.TotalFailures = prCount + commitCount;
            data.CommitFailures = commitCount;
            data.PullRequestFailures = prCount;
            return data;
        }

        [Route("api/builds/viewNames")]
        public List<string> GetViewNames([FromUri] DateTimeOffset? startDate = null)
        {
            var startDateValue = startDate ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            return _buildUtil.GetViewNames(startDateValue);
        }
    }
}