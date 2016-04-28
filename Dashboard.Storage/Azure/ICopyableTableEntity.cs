using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure
{
    public interface ICopyableTableEntity<T>
        where T : ITableEntity
    {
        T Copy(EntityKey key);
    }
}
