using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Dashboard.Controllers
{
    public class ContentData
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// This is a proof of concept implementation only.  I realize that its implementation is pretty terrible
    /// and that's fine.  For now it is enough to validate the end to end scenario.
    /// </summary>
    public class TestCacheController : ApiController
    {
        private const int MapLimit = 500;
        private static Dictionary<string, string> s_cacheMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private void Add(string key, string value)
        {
            if (value.Length > 50000)
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

        public string Get(string id)
        {
            lock (s_cacheMap)
            {
                return s_cacheMap[id];
            }
        }

        public void Post(ContentData contentData)
        {
            Add(contentData.Key, contentData.Value);
        }

        public void Put(string id, [FromBody]string content)
        {
            Add(id, content);
        }

        // TODO
        public void Delete(int id)
        {

        }
    }
}