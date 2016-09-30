using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Dashboard.Azure
{
    public abstract class CounterEntity : TableEntity
    {
        public bool IsJenkins { get; set; }
        public long DateTimeUtcTicks { get; set; }

        /// <summary>
        /// Id representing the source which is updating this particular counter entity.  Allows for 
        /// multiple entities to be updating the same time interval in parallel.  Each is stored as a 
        /// separate entity so there is no write contention.
        /// </summary>
        public string EntityWriterId { get; set; }

        public DateTimeOffset DateTime => new DateTimeOffset(DateTimeUtcTicks, TimeSpan.Zero);

        protected CounterEntity()
        {

        }

        protected CounterEntity(CounterData data)
        {
            var key = CounterUtil.GetEntityKey(data);
            PartitionKey = key.PartitionKey;
            RowKey = key.RowKey;
            IsJenkins = data.IsJenkins;
            DateTimeUtcTicks = data.DateTime.UtcTicks;
            EntityWriterId = data.EntityWriterId;
        }
    }
}
