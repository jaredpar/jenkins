using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Helpers
{
    // TODO: should have separate objects for json serialization and storage.
    public class TestCacheData
    {
        public int ExitCode { get; set; }
        public string OutputStandard { get; set; }
        public string OutputError { get; set; }
        public string ResultsFileName { get; set; }
        public string ResultsFileContent { get; set; }
    }

    /// <summary>
    /// This is a proof of concept implementation only.  I realize that its implementation is pretty terrible
    /// and that's fine.  For now it is enough to validate the end to end scenario.
    /// </summary>
    public class TestCacheStorage
    {
        public static TestCacheStorage Instance = new TestCacheStorage();

        private const int MapLimit = 500;
        private const int SizeLimit = 10000000;

        private readonly Dictionary<string, TestCacheData> _testCacheDataMap = new Dictionary<string, TestCacheData>();

        public List<string> Keys
        {
            get
            {
                lock (_testCacheDataMap)
                {
                    return _testCacheDataMap.Keys.ToList();
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_testCacheDataMap)
                {
                    return _testCacheDataMap.Count;
                }
            }
        }

        private TestCacheStorage()
        {

        }

        public void Add(string key, TestCacheData value)
        {
            if (string.IsNullOrEmpty(value.ResultsFileContent) || value.ResultsFileContent.Length > SizeLimit)
            {
                throw new Exception("Data too big");
            }

            lock (_testCacheDataMap)
            {
                _testCacheDataMap[key] = value;
                if (_testCacheDataMap.Count > MapLimit)
                {
                    var toRemove = _testCacheDataMap.Keys.Take(MapLimit / 5);
                    foreach (var item in toRemove)
                    {
                        _testCacheDataMap.Remove(item);
                    }
                }
            }
        }

        public bool TryGetValue(string key, out TestCacheData testCacheData)
        {
            lock (_testCacheDataMap)
            {
                return _testCacheDataMap.TryGetValue(key, out testCacheData);
            }
        }
    }
}