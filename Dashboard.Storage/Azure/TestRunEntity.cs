using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    /// <summary>
    /// Information about a test run.
    /// </summary>
    public sealed class TestRunEntity : TableEntity
    {
        public string MachineName { get; set; }
        public string EnlistmentRoot { get; set; }
        public DateTime RunDate { get; set; }
        public string CacheType { get; set; }
        public long ElapsedSeconds { get; set; }
        public bool Succeeded { get; set; }
        public bool IsJenkins { get; set; }
        public bool Is32Bit { get; set; }
        public int AssemblyCount { get; set; }
        public int CacheCount { get; set; }
        public int ChunkCount { get; set; }
        public string JenkinsUrl { get; set; }
        public bool HasErrors { get; set; }

        public TimeSpan Elapsed => TimeSpan.FromSeconds(ElapsedSeconds);

        public TestRunEntity()
        {

        }

        public TestRunEntity(DateTime runDate, BuildSource buildSource)
        {
            PartitionKey = GetPartitionKey(runDate);
            RowKey = GetRowKey(RunDate, buildSource);
            MachineName = buildSource.MachineName;
            EnlistmentRoot = buildSource.EnlistmentRoot;
            RunDate = runDate;
        }

        public static string GetPartitionKey(DateTime runDate)
        {
            Debug.Assert(runDate.Kind == DateTimeKind.Utc);
            var date = runDate.Date;
            return date.ToString("yyyy-MM-dd");
        }

        public static string GetRowKey(DateTime runDate, BuildSource buildSource)
        {
            var machineName = AzureUtil.NormalizeKey(buildSource.MachineName, '-');
            return $"{machineName}-{(long)runDate.Ticks}";
        }

        public EntityKey CreateEntityKey(DateTime runDate, BuildSource buildSource)
        {
            return new EntityKey(
                GetPartitionKey(runDate),
                GetRowKey(runDate, buildSource));
        }
    }
}
