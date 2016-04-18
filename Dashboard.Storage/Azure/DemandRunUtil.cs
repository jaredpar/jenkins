using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage.Table;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public sealed class DemandRunUtil
    {
        private readonly DashboardStorage _storage;

        public DemandRunUtil(DashboardStorage storage)
        {
            _storage = storage;
        }

        public async Task CreateDemandRun(Uri jenkinsUrl, string userName, string token, Uri repoUrl, string commit, List<JobId> jobs)
        {
            var builder = new DemandBuildBuilder(jenkinsUrl, userName, token, repoUrl, commit);
            var list = new List<DemandBuildEntity>();
            foreach (var id in jobs)
            {
                var entity = await builder.CreateDemandBuild(id);
                list.Add(entity);
            }

            await AzureUtil.InsertBatch(_storage.DemandBuildTable, list);

            var runEntity = new DemandRunEntity(userName, commit, repoUrl);
            var operation = TableOperation.InsertOrReplace(runEntity);
            _storage.DemandRunTable.Execute(operation);
        }
    }
}
