using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public sealed class DashboardStorage
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly CloudTable _buildFailureTable;
        private readonly CloudTable _buildProcessedTable;

        public DashboardStorage(string connectionString)
        {
            _storageAccount = CloudStorageAccount.Parse(connectionString);

            var tableClient = _storageAccount.CreateCloudTableClient();
            _buildFailureTable = tableClient.GetTableReference(AzureConstants.TableNameBuildFailure);
            _buildProcessedTable = tableClient.GetTableReference(AzureConstants.TableNameBuildProcessed);
        }

        public IEnumerable<BuildFailureEntity> GetBuildFailureEntities(DateTime? startDate = null)
        { 
            var startDateValue = GetStartDateValue(startDate);
            var query = new TableQuery<BuildFailureEntity>().Where(GenerateFilterBuildFailureDate(startDateValue));
            return _buildFailureTable.ExecuteQuery(query);
        }

        /// <summary>
        /// Get the <see cref="BuildFailureEntity"/> values for a given job since the specified date.
        /// </summary>
        public IEnumerable<BuildFailureEntity> GetBuildFailureEntities(string name, DateTime? startDate = null)
        {
            var startDateValue = GetStartDateValue(startDate);
            var dateFilter = GenerateFilterBuildFailureDate(startDateValue);
            var rowFilter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.RowKey),
                QueryComparisons.Equal,
                name);
            var query = new TableQuery<BuildFailureEntity>().Where(TableQuery.CombineFilters(rowFilter, TableOperators.And, dateFilter));
            return _buildFailureTable.ExecuteQuery(query);
        }

        public DateTime GetStartDateValue(DateTime? startDate)
        {
            return startDate?.ToUniversalTime().Date ?? DateTime.UtcNow.Date - TimeSpan.FromDays(1);
        }

        public static string GenerateFilterBuildFailureDate(DateTime startDate)
        {
            Debug.Assert(startDate.Kind == DateTimeKind.Utc);
            return TableQuery.GenerateFilterConditionForDate(nameof(BuildFailureEntity.BuildDate), QueryComparisons.GreaterThanOrEqual, new DateTimeOffset(startDate));
        }
    }
}
