using Dashboard.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace Dashboard.Controllers
{
    [RoutePrefix("api/testData")]
    public class TestDataController : DashboardApiController
    {
        [Route("cache")]
        [HttpGet]
        public string Test()
        {
            return "Cache";
        }

        [Route("cache")]
        [HttpPut]
        public void OtherTest(string data)
        {

        }

        [Route("run")]
        [HttpGet]
        public string RunTest()
        {
            return "run";
        }

        [Route("run")]
        public void PutTest(string data)
        {

        }
    }
}