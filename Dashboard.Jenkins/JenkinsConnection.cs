using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Jenkins
{
    internal sealed class JenkinsConnection
    {
        private readonly Uri _baseUrl;
        private readonly IRestClient _restClient;
        private readonly string _authorizationHeaderValue;

        internal IRestClient RestClient => _restClient;

        internal JenkinsConnection(Uri baseUrl, string username = null, string password = null) 
            : this(baseUrl, new RestClient(), username, password)
        {

        }

        internal JenkinsConnection(Uri baseUrl, IRestClient restClient, string userName = null, string password = null)
        {
            _baseUrl = baseUrl;
            _restClient = restClient;

            if (userName != null || password != null)
            {
                _authorizationHeaderValue = SharedUtil.CreateAuthorizationHeader(userName, password);
            }
        }

        internal JObject GetJson(string urlPath, bool pretty = false, string tree = null, int? depth = null)
        {
            var request = GetJsonRestRequest(urlPath, pretty, tree, depth);
            var response = _restClient.Execute(request);
            return ParseJsonCore(response);
        }

        internal async Task<JObject> GetJsonAsync(string urlPath, bool pretty = false, string tree = null, int? depth = null)
        {
            var request = GetJsonRestRequest(urlPath, pretty, tree, depth);
            var response = await _restClient.ExecuteTaskAsync(request);
            return ParseJsonCore(response);
        }

        /// <summary>
        /// Build up the <see cref="RestRequest"/> object for the JSON query. 
        /// </summary>
        internal RestRequest GetJsonRestRequest(string urlPath, bool pretty, string tree, int? depth)
        {
            urlPath = urlPath.TrimEnd('/');
            var request = new RestRequest($"{urlPath}/api/json", Method.GET);
            request.AddParameter("pretty", pretty ? "true" : "false");

            if (depth.HasValue)
            {
                request.AddParameter("depth", depth);
            }

            if (!string.IsNullOrEmpty(tree))
            {
                request.AddParameter("tree", tree);
            }

            if (!string.IsNullOrEmpty(_authorizationHeaderValue))
            {
                request.AddHeader("Authorization", _authorizationHeaderValue);
            }

            return request;
        }

        private static JObject ParseJsonCore(IRestResponse response)
        {
            try
            {
                return JObject.Parse(response.Content);
            }
            catch (Exception e)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"Unable to parse json");
                builder.AppendLine($"  Url: {response.ResponseUri}");
                builder.AppendLine($"  Status: {response.StatusDescription}");
                builder.AppendLine($"  Conent: {response.Content}");
                throw new Exception(builder.ToString(), e);
            }
        }

    }
}
