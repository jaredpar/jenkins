using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Sql
{
    public struct StorageLoggerEntry
    {
        public readonly string Category;
        public readonly string Message;

        public StorageLoggerEntry(string category, string message)
        {
            Category = category;
            Message = message;
        }
    }

    public sealed class StorageLogger : ILogger
    {
        public static readonly StorageLogger Instance = new StorageLogger();
        private readonly List<StorageLoggerEntry> _list = new List<StorageLoggerEntry>();
        private readonly int _max = 500;

        public List<StorageLoggerEntry> EntryList
        {
            get
            {
                lock (_list)
                {
                    return new List<StorageLoggerEntry>(_list);
                }
            }
        }

        private void LogCore(string category, string message)
        {
            lock (_list)
            {
                _list.Add(new StorageLoggerEntry(category, message));

                while (_list.Count > _max)
                {
                    _list.RemoveRange(0, _max / 5);
                }
            }
        }

        void ILogger.Log(string category, string message)
        {
            LogCore(category, message);
        }

        void ILogger.Log(string category, string message, Exception ex)
        {
            LogCore(category, $"{message}: {ex.Message}");
        }
    }
}
