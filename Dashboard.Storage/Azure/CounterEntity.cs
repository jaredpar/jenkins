using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public abstract class CounterEntity : TableEntity
    {
        public bool IsJenkins { get; set; }
        public long TimeOfDayTicks { get; set; }

        /// <summary>
        /// Id representing the source which is updating this particular counter entity.  Allows for 
        /// multiple entities to be updating the same time interval in parallel.  Each is stored as a 
        /// separate entity so there is no write contention.
        /// </summary>
        public string EntityWriterId => CounterUtil.GetEntityWriterId(RowKey);
        public DateTime Date => DateTime.Parse(PartitionKey);
        public TimeSpan TimeOfDay => TimeSpan.FromTicks(TimeOfDayTicks);

        protected CounterEntity()
        {

        }

        protected CounterEntity(CounterData data)
        {
            var key = CounterUtil.GetEntityKey(data);
            PartitionKey = key.PartitionKey;
            RowKey = key.RowKey;
            IsJenkins = data.IsJenkins;
            TimeOfDayTicks = data.TimeOfDayTicks;
        }
    }
}
