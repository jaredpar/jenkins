using Microsoft.WindowsAzure.Storage.Table;

namespace Dashboard.Azure.Builds
{
    /// <summary>
    /// Used to store the set of available view name values for a given date.  It has the same data as 
    /// <see cref="BuildResultEntity"/> in the date table but in a more querable form.
    /// </summary>
    public sealed class ViewNameEntity : TableEntity
    {
        public DateKey DateKey => DateKey.Parse(PartitionKey);
        public string ViewName => RowKey;

        public ViewNameEntity()
        {

        }

        public ViewNameEntity(DateKey dateKey, string viewName)
        {
            PartitionKey = dateKey.Key;
            RowKey = viewName;
        }
    }
}
