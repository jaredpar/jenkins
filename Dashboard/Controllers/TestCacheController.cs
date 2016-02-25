using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Dashboard.Controllers
{
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
    public class TestCacheController : ApiController
    {
        private const int MapLimit = 500;
        private static Dictionary<string, TestCacheData> s_cacheMap = new Dictionary<string, TestCacheData>(StringComparer.OrdinalIgnoreCase);

        private void Add(string key, TestCacheData value)
        {
            if (string.IsNullOrEmpty(value.ResultsFileContent) || value.ResultsFileContent.Length > 100000)
            {
                throw new Exception("Data too big");
            }

            lock (s_cacheMap)
            {
                s_cacheMap[key] = value;
                if (s_cacheMap.Count > MapLimit)
                {
                    var toRemove = s_cacheMap.Keys.Take(MapLimit / 5);
                    foreach (var item in toRemove)
                    {
                        s_cacheMap.Remove(item);
                    }
                }
            }
        }

        public IEnumerable<string> Get()
        {
            lock (s_cacheMap)
            {
                return s_cacheMap.Keys.ToList();
            }
        }

        public TestCacheData Get(string id)
        {
            lock (s_cacheMap)
            {
                return s_cacheMap[id];
            }
        }

        /*
        public void Post(TestCacheData testCacheData)
        {
            Add(testCacheData
        }
        */

        public void Put(string id, [FromBody]TestCacheData testCacheData)
        {
            Add(id, testCacheData);
        }

        // TODO
        public void Delete(int id)
        {

        }
    }
}