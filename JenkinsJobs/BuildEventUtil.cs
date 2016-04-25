using Dashboard.Azure;
using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard.StorageBuilder
{
    internal sealed class BuildEventUtil
    {
        private readonly CloudTable _buildEventTable;
        private readonly CloudTable _buildProcessedTable;
        private readonly CloudTable _buildFailureTable;
        private readonly TextWriter _textWriter;
        private readonly string _githubConnectionString;

        internal BuildEventUtil(CloudTable buildEventTable, CloudTable buildProcessedTable, CloudTable buildFailureTable, TextWriter textWriter, string githubConnectionString)
        {
            _buildEventTable = buildEventTable;
            _buildProcessedTable = buildProcessedTable;
            _buildFailureTable = buildFailureTable;
            _githubConnectionString = githubConnectionString;
            _textWriter = textWriter;
        }

        internal async Task<List<BuildAnalyzeError>> Process(string message, CancellationToken cancellationToken)
        {
            // Ensure there is an entry in the build table noting this build event.  If the actual processing fails later jobs 
            // can come and clean this up. 
            var entity = CreateBuildEventEntity(message);
            await _buildEventTable.ExecuteAsync(TableOperation.InsertOrReplace(entity), cancellationToken);

            // No point is trying to get build result information until the job is done.
            if (entity.Phase != "COMPLETED" && entity.Phase != "FINALIZED")
            {
                return new List<BuildAnalyzeError>();
            }

            var jenkinsUri = new Uri($"https://{entity.JenkinsHostName}");
            var jenkinsClient = new JenkinsClient(jenkinsUri, connectionString: _githubConnectionString); 
            var jobTableUtil = new JobTableUtil(buildProcessedTable: _buildProcessedTable, buildFailureTable: _buildFailureTable, client: jenkinsClient, textWriter: _textWriter);
            await jobTableUtil.PopulateBuildAsync(entity.BuildId);

            // Now that the build is processed delete the entity from the event table to signify that it no longer needs
            // to be processed.
            await _buildEventTable.ExecuteAsync(TableOperation.Delete(entity), cancellationToken);
            return jobTableUtil.BuildAnalyzeErrors.ToList();
        }

        private BuildEventEntity CreateBuildEventEntity(string message)
        { 
            var messageJson = (MessageJson)JsonConvert.DeserializeObject(message, typeof(MessageJson));
            var jobId = JenkinsUtil.ConvertPathToJobId(messageJson.JobName);
            var uri = new Uri(messageJson.Url);
            return new BuildEventEntity(new BuildId(messageJson.Number, jobId))
            {
                JenkinsHostName = uri.Host,
                QueueId = messageJson.QueueId,
                Status = messageJson.Status,
                Phase = messageJson.Phase
            };
        }
    }
}
