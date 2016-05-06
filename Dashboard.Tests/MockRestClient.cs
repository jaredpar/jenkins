using Dashboard.Jenkins;
using Moq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Tests
{
    internal sealed class MockRestClient
    {
        private readonly Dictionary<string, string> _map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Mock<IRestClient> _client = new Mock<IRestClient>();

        internal Mock<IRestClient> ClientMock => _client;
        internal IRestClient Client => _client.Object;

        internal MockRestClient()
        {
            _client
                .Setup(x => x.ExecuteTaskAsync(It.IsAny<IRestRequest>()))
                .Returns<IRestRequest>(HandleExecuteTaskAsync);

            _client
                .Setup(x => x.Execute(It.IsAny<IRestRequest>()))
                .Returns<IRestRequest>(HandleExecute);
        }

        internal void AddJson(
            BuildId buildId,
            string buildInfoJson = null,
            string buildResultJson = null,
            string failureInfoJson = null,
            string testReportJson = null)
        {
            var buildPath = $"{JenkinsUtil.GetBuildPath(buildId)}api/json";
            if (buildInfoJson != null)
            {
                AddJsonCore(buildInfoJson, buildPath, tree: JsonUtil.BuildInfoTreeFilter);
            }

            if (buildResultJson != null)
            {
                AddJsonCore(buildResultJson, buildPath);
            }

            if (failureInfoJson != null)
            {
                AddJsonCore(failureInfoJson, buildPath, depth: 4);
            }

            if (testReportJson != null)
            {
                var testReportPath = $"{JenkinsUtil.GetBuildPath(buildId)}testReport/api/json";
                AddJsonCore(testReportJson, testReportPath);
            }
        }

        private void AddJsonCore(string json, string urlPath, bool pretty = false, string tree = null, int depth = 1)
        {
            var key = GetKey(urlPath, pretty.ToString(), tree, depth.ToString());
            _map.Add(key, json);
        }

        private string GetApiPath(BuildId buildId)
        {
            return $"{JenkinsUtil.GetBuildPath(buildId)}api/json";
        }

        private string GetKey(string urlPath, string pretty, string tree, string depth)
        {
            return $"{urlPath}?pretty={pretty}&tree={tree}&depth={depth}";
        }

        private string GetKey(IRestRequest request)
        {
            var urlPath = request.Resource;
            var pretty = GetParameter(request, "pretty", false);
            var tree = GetParameter(request, "tree", (string)null);
            var depth = GetParameter(request, "depth", 1);
            return GetKey(urlPath, pretty, tree, depth);
        }

        private string GetParameter<T>(IRestRequest request, string name, T defaultValue)
        {
            var obj = request
                .Parameters
                .Where(x => x.Name == name)
                .Select(x => x.Value)
                .FirstOrDefault();
            var value = obj != null ? obj : defaultValue;
            return value?.ToString();
        }

        private IRestResponse HandleExecute(IRestRequest request)
        {
            var key = GetKey(request);
            var content = _map[key];
            var mock = new Mock<IRestResponse>();
            mock.SetupGet(x => x.Content).Returns(content);
            return mock.Object;
        }

        private Task<IRestResponse> HandleExecuteTaskAsync(IRestRequest request)
        {
            return Task.FromResult(HandleExecute(request));
        }
    }
}
