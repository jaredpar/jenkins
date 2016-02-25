using Dashboard.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Dashboard.Controllers
{
    /// <summary>
    /// This is a proof of concept implementation only.  I realize that its implementation is pretty terrible
    /// and that's fine.  For now it is enough to validate the end to end scenario.
    /// </summary>
    public class TestCacheController : ApiController
    {
        private TestCacheStorage _storage = TestCacheStorage.Instance;
        private TestCacheStats _stats = TestCacheStats.Instance;

        public IEnumerable<string> Get()
        {
            return _storage.Keys;
        }

        public TestCacheData Get(string id)
        {
            TestCacheData testCacheData;
            if (_storage.TryGetValue(id, out testCacheData))
            {
                _stats.AddHit();
                return testCacheData;
            }

            _stats.AddMiss();
            throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        /*
        public void Post(TestCacheData testCacheData)
        {
            Add(testCacheData
        }
        */

        public void Put(string id, [FromBody]TestCacheData testCacheData)
        {
            _storage.Add(id, testCacheData);
            _stats.AddStore();
        }

        // TODO
        public void Delete(int id)
        {

        }
    }
}