using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;

namespace Dashboard.Tests
{
    public class JenkinsClientTests
    {
        private readonly MockRestClient _restClient = new MockRestClient();
        private readonly JenkinsClient _client;

        protected JenkinsClientTests()
        {
            _client = new JenkinsClient(new Uri("http://invalid.com"), _restClient.Client);
        }

        public sealed class GetFailedTestCases : JenkinsClientTests
        {
            [Fact]
            public void ParseNonJson()
            {
                var buildId = new BuildId(42, JobId.ParseName("test"));
                _restClient.AddJson(buildId, testReportJson: TestResources.TestReport1);
                Assert.Throws<JsonReaderException>(() => _client.GetFailedTestCases(buildId));
            }
        }
    }
}
