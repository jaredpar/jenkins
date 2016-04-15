using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    // TODO: This logic should be in JsonUtil
    public static class BuildFailureUtil
    {
        /// <summary>
        /// Parse out the identified known causes.  This needs to be kept up to date with the following:
        ///     http://dotnet-ci.cloudapp.net/failure-cause-management/
        /// </summary>
        public static bool TryGetBuildFailureInfo(JObject jobData, out BuildFailureInfo buildFailureInfo)
        {
            var actions = (JArray)jobData["actions"];
            foreach (var cur in actions)
            {
                var foundCauses = (JArray)cur["foundFailureCauses"];
                if (foundCauses != null && TryGetBuildFailureInfoCustomCauses(foundCauses, out buildFailureInfo))
                {
                    return true;
                }

                var causes = (JArray)cur["causes"];
                if (causes != null && IsMergeConflict(causes))
                {
                    buildFailureInfo = BuildFailureInfo.MergeConflict;
                    return true;
                }

                if (cur is JObject && TryGetTestFailureInfo((JObject)cur, out buildFailureInfo))
                {
                    return true;
                }
            }

            buildFailureInfo = null;
            return false;
        }

        private static bool TryGetBuildFailureInfoCustomCauses(JArray foundCauses, out BuildFailureInfo buildFailureInfo)
        {
            foreach (JObject entry in foundCauses)
            {
                var category = GetCategory(entry);
                if (category == null)
                {
                    continue;
                }

                var description = entry.Value<string>("description");
                var name = entry.Value<string>("name");
                buildFailureInfo = new BuildFailureInfo(name: name, description: description, category: category.Value);
                return true;
            }

            buildFailureInfo = null;
            return false;
        }

        private static bool IsMergeConflict(JArray causes)
        {
            foreach (JObject obj in causes)
            {
                var value = obj.Value<string>("shortDescription");
                if (value != null && value.Contains("has merge conflicts"))
                {
                    return true;
                }
            }

            return false;
        }

        private static BuildFailureCategory? GetCategory(JObject causeItem)
        {
            switch (GetCategoryRaw(causeItem).ToLower())
            {
                case "build": return BuildFailureCategory.Build;
                case "infrastructure": return BuildFailureCategory.Infrastructure;
                case "test": return BuildFailureCategory.TestCase;
                default: return null;
            }
        }

        private static string GetCategoryRaw(JObject causeItem)
        {
            var items = (JArray)causeItem["categories"];
            if (items.Count == 0)
            {
                return null;
            }

            return items[0].Value<string>();
        }

        /// <summary>
        /// Convert to a unit test entry if this matches.
        /// </summary>
        private static bool TryGetTestFailureInfo(JObject data, out BuildFailureInfo failureInfo)
        {
            failureInfo = null;

            // The JSON looks like teh following:
            // {    "failCount" : 1,
            //      "skipCount" : 2546,
            //      "totalCount" : 66764,
            //      "urlName" : "testReport" }
            var urlName = data.Value<string>("urlName");
            if (string.IsNullOrEmpty(urlName))
            {
                return false;
            }

            var failCount = data.Value<int?>("failCount");
            if (!failCount.HasValue)
            {
                return false;
            }

            var message = $"Unit Test Failure: {failCount}";
            failureInfo = new BuildFailureInfo("Unit Test", message, BuildFailureCategory.TestCase);
            return true;
        }

        /// <summary>
        /// Parse out the testReport data for a given job.
        /// </summary>
        public static bool TryGetTestCaseFailureList(JObject data, out List<string> testCaseList)
        {
            var failCount = data.Value<int?>("failCount");
            if (failCount == null || failCount.Value == 0)
            {
                testCaseList = null;
                return false;
            }

            testCaseList = new List<string>();
            var suites = (JArray)data["suites"];
            foreach (var suite in suites)
            {
                var cases = (JArray)suite["cases"];
                foreach (var cur in cases)
                {
                    var status = cur.Value<string>("status");
                    if (status == "PASSED" || status == "SKIPPED" || status == "FIXED")
                    {
                        continue;
                    }

                    var className = cur.Value<string>("className");
                    var name = cur.Value<string>("name");
                    testCaseList.Add($"{className}.{name}");
                }
            }

            return true;
        }
    }
}
