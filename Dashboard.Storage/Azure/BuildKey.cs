using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public struct BuildKey
    {
        public BuildId BuildId { get; }

        public string Key => $"{BuildId.Id}-{BuildId.JobName}";

        public BuildKey(BuildId buildId)
        {
            BuildId = buildId;
        }

        public static BuildKey Parse(string key)
        {
            var items = key.Split(new [] { '-' }, count: 2, options: StringSplitOptions.RemoveEmptyEntries);
            var number = int.Parse(items[0]);
            var jobId = JobId.ParseName(items[1]);
            return new BuildKey(new BuildId(number, jobId));
        }
    }
}
