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

namespace Dashboard.Controllers
{
    public class BuildsApiController : ApiController
    {
        private readonly DashboardStorage _storage;

        public BuildsApiController()
        {
            var connectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageConnectionStringName);
            _storage = new DashboardStorage(connectionString);
        }

        /// <summary>
        /// Get all of the the test based failures since the provided date.  Default is 1 day. 
        /// </summary>
        [Route("api/builds/testFailures")]
        public List<TestFailureData> GetTestFailures([FromUri] DateTime? startDate = null)
        {
            var list = new List<TestFailureData>();
            foreach (var group in _storage.GetBuildFailureEntities(startDate).GroupBy(x => x.Identifier))
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
        public TestFailureData GetTestFailure([FromUri] string name, [FromUri] DateTime? startDate = null)
        {
            var startDateValue = _storage.GetStartDateValue(startDate);
            var data = new TestFailureData()
            {
                Name = name,
                Builds = new List<BuildData>()
            };

            var prCount = 0;
            var commitCount = 0;
            foreach (var entity in _storage.GetBuildFailureEntities(name, startDate))
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
                    JobUri = JenkinsUtil.GetUri(SharedConstants.DotnetJenkinsUri, JenkinsUtil.GetJobIdPath(jobId)).ToString(),
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

        // DEMAND: should return the URI they can use for getting updates
        [Route("api/builds/demand")]
        public async Task CreateDemandBuild(DemandBuildModel model)
        {
            await InsertBuilds(model);

            var entity = new DemandRunEntity(model.UserName, model.Sha1, new Uri(model.RepoUrl));
            var operation = TableOperation.Insert(entity);
            _storage.DemandRunTable.Execute(operation);
        }

        private async Task InsertBuilds(DemandBuildModel model)
        {
            var list = new List<DemandBuildEntity>();
            foreach (var item in model.QueuedItems)
            {
                var entity = new DemandBuildEntity(model.UserName, model.Sha1, item.QueueItemNumber, item.JobName);
                list.Add(entity);
            }

            await AzureUtil.InsertBatch(_storage.DemandBuildTable, list);
        }
    }
}