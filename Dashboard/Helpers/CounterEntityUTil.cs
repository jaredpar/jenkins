using Dashboard.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace Dashboard.Helpers
{
    public class CounterEntityUtil<T>
        where T : CounterEntity, new()
    {
        private static readonly object s_guard = new object();
        private static readonly Guid s_idBase = Guid.NewGuid();
        private static T s_entity;

        private readonly CloudTable _table;
        private readonly Func<CounterData, T> _createFunc;

        public CounterEntityUtil(CloudTable table, Func<CounterData, T> createFunc)
        {
            _table = table;
            _createFunc = createFunc;
        }

        public void Update(T entity)
        {
            var operation = TableOperation.InsertOrReplace(entity);
            _table.Execute(operation);
        }

        public T GetEntity(bool isJenkins)
        {
            var counterData = new CounterData(GetEntityWriterId(), isJenkins);
            var key = counterData.EntityKey;
            lock (s_guard)
            {
                if (s_entity != null && s_entity.GetEntityKey() == key)
                {
                    return s_entity;
                }
            }

            var entity = GetOrCreateEntity(counterData);
            lock (s_guard)
            {
                s_entity = entity;
            }

            return entity;
        }

        private T GetOrCreateEntity(CounterData counterData)
        {
            var key = CounterUtil.GetEntityKey(counterData);
            var filter = TableQueryUtil.Key(key);
            var query = new TableQuery<T>().Where(filter);
            var entity = _table.ExecuteQuery(query).FirstOrDefault();
            if (entity != null)
            {
                return entity;
            }

            return _createFunc(counterData);
        }

        private static string GetEntityWriterId() => $"{s_idBase}-{Thread.CurrentThread.ManagedThreadId}";
    }
}