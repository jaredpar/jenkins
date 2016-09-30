using Dashboard.Jenkins;
using Microsoft.WindowsAzure.Storage.Table;

namespace Dashboard.Azure.Builds
{
    /// <summary>
    /// Tracks the status of a build as it comes through our build event queue.  It's a back up in the case 
    /// we lose events from Jenkins: either through Jenkins not sending all events or in the case the web
    /// site goes down for a few seconds.
    ///
    /// Every row in this table represents a build that we do not yet considered processsed.  Once processed
    /// we delete the row and consider the build done.  This is kept separate from the <see cref="BuildProcessedEntity"/>
    /// table because occasionally we need to do full scans to find items that fell off the map.  This table
    /// is kept small to make that efficient.
    /// </summary>
    public class BuildEventEntity : TableEntity
    {
        public const string TableName = AzureConstants.TableNames.BuildEvent;

        public string JenkinsHostName { get; set; }
        public int QueueId { get; set; }
        public string Phase { get; set; }
        public string Status { get; set; }

        public BuildId BuildId => new BuildId(int.Parse(RowKey), JobId.ParseName(PartitionKey));

        public BuildEventEntity()
        {

        }

        public BuildEventEntity(BuildId id)
        {
            var key = GetEntityKey(id);
            PartitionKey = key.PartitionKey;
            RowKey = key.RowKey;
        }

        public static EntityKey GetEntityKey(BuildId id)
        {
            return new EntityKey(id.JobName.ToString(), id.Number.ToString());
        }
    }
}
