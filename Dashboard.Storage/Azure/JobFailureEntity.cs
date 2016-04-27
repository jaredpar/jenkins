using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public sealed class JobFailureEntity : TableEntity
    {
        public const string TableName = AzureConstants.TableNames.JobFailure;

        public string Category { get; set; }
        public int BuildNumber { get; set; }
        public string JobName { get; set; }
        public DateTime BuildDateTime { get; set; }
    }
}
