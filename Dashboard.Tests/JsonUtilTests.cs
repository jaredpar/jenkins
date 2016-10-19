using Dashboard.Jenkins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Xunit;

namespace Dashboard.Tests
{
    public abstract class JsonUtilTests
    {
        public sealed class BulidFailureParsingTests
        {
            [Fact]
            public void TaoTestCase()
            {
                var json = JObject.Parse(TestResources.BuildFailure1);
                var result = JsonUtil.ParseBuildFailureInfo(json);
                Assert.Equal(1, result.CauseList.Count);
                Assert.Equal("Test", result.CauseList[0].Category);
            }

            [Fact]
            public void UnitTestFailure()
            {
                var json = JObject.Parse(TestResources.BuildFailure2);
                var result = JsonUtil.ParseBuildFailureInfo(json);
                Assert.Equal(1, result.CauseList.Count);
                Assert.Equal("Test", result.CauseList[0].Category);
            }

            [Fact]
            public void AlreadyMergedCause()
            {
                var json = JObject.Parse(TestResources.BuildFailure3);
                var result = JsonUtil.ParseBuildFailureInfo(json);
                Assert.Equal(1, result.CauseList.Count);
            }
        }

        public sealed class ParseTestCaseLog
        {
            [Fact]
            public void TaoReport()
            {
                var list = JsonUtil.ParseTestCaseListFailed(new JsonTextReader(new StringReader(TestResources.Tao1TestResult)));
                Assert.Equal(2, list.Count);
            }

            [Fact]
            public void MultipleSuites()
            {
                var list = JsonUtil.ParseTestCaseList(new JsonTextReader(new StringReader(TestResources.TestReport2)));
                Assert.Equal(2, list.Count);
            }
        }
       
    }
}
