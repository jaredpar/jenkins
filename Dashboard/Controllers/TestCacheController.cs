using Dashboard.Helpers;
using Dashboard.Models;
using Dashboard;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.WindowsAzure;
using Dashboard.Azure;
using Dashboard.Helpers.Json;

namespace Dashboard.Controllers
{
    /// <summary>
    /// TODO: Delete this controller.
    ///
    /// This is a legacy controller that exists purely to keep old clients happy.
    /// </summary>
    public class TestCacheController : ApiController
    {
        private readonly TestDataController _testDataController = new TestDataController();

        public TestResultData Get(string id, [FromUri] TestSourceData testSourceData)
        {
            return _testDataController.GetTestCache(id, testSourceData);
        }

        public void Put(string id, [FromBody] TestCacheData testCacheData)
        {
            _testDataController.PutTestCache(id, testCacheData);
        }
    }
}