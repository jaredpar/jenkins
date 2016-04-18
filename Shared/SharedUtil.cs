using RestSharp;
using System;
using System.Text;

namespace Dashboard
{
    internal static class SharedUtil
    {
        internal static string CreateAuthorizationHeader(string userName, string password)
        {
            var bytes = Encoding.UTF8.GetBytes($"{userName}:{password}");
            var encoded = Convert.ToBase64String(bytes);
            return $"Basic {encoded}";
        }

        internal static void AddAuthorization(RestRequest request, string userName, string password)
        {
            var header = CreateAuthorizationHeader(userName, password);
            request.AddHeader("Authorization", header);
        }
    }
}
