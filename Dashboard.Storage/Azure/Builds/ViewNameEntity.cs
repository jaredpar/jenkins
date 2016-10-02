using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Dashboard.Azure.Builds
{
    /// <summary>
    /// Used to store the set of available view name values for a given date.  It has the same data as 
    /// <see cref="BuildResultEntity"/> in the date table but in a more querable form.
    /// </summary>
    public sealed class ViewNameEntity : TableEntity
    {
        public DateTimeKey DateTimeKey => DateTimeKey.ParseDateTimeKey(PartitionKey, DateTimeKeyFlags.Date);
        public string ViewName => RowKey;

        public ViewNameEntity()
        {

        }

        public ViewNameEntity(DateTimeOffset date, string viewName)
        {
            PartitionKey = DateTimeKey.GetDateKey(date);
            RowKey = viewName;
        }
    }
}
