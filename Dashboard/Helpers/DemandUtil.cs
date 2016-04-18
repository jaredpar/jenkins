using Dashboard.Azure;
using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Helpers
{
    // DEMAND: this is a really hack class that manually moves the state of a demand build forward.  Really
    // web jobs should control this.
    public class DemandUtil
    {
        private readonly DashboardStorage _storage;

        public DemandUtil(DashboardStorage storage)
        {
            _storage = storage;
        }

        public void MoveQueueToCreated(string userName, string commit)
        {
            var query = new TableQuery<DemandBuildEntity>()
                .Where(DashboardStorage.GenerateDemandBuildFilter(userName, commit));
            var client = new JenkinsClient(SharedConstants.DotnetJenkinsUri);
            foreach (var entity in _storage.DemandBuildTable.ExecuteQuery(query))
            {
                var info = client.GetQueuedItemInfo(entity.QueueItemNumber);
                if (info.BuildNumber.HasValue)
                {
                    entity.BuildNumber = info.BuildNumber.Value;
                    entity.StatusRaw = DemandBuildStatus.Running.ToString();
                    var operation = TableOperation.InsertOrReplace(entity);
                    _storage.DemandBuildTable.Execute(operation);
                }
            }
        }
    }
}