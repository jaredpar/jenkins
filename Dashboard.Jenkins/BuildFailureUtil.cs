using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Jenkins
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
            var causeList = new List<BuildFailureCause>();
            if (TryGetFailureCauses(actions, causeList) ||
                TryGetUnitTestCauses(actions, causeList) ||
                TryGetMergeConflict(actions, causeList))
            {
                buildFailureInfo = new BuildFailureInfo(new ReadOnlyCollection<BuildFailureCause>(causeList));
                return true;
            }

            buildFailureInfo = null;
            return false;
        }

        /// <summary>
        /// Look at the "foundFailureCauses" member of the JSON.  This is the custom failures that Jenkins
        /// admins have assigned via regex.  
        ///     http://dotnet-ci.cloudapp.net/failure-cause-management/
        /// </summary>
        private static bool TryGetFailureCauses(JArray actions, List<BuildFailureCause> causeList)
        {
            var any = false;
            foreach (var cur in actions)
            {
                var foundCauses = (JArray)cur["foundFailureCauses"];
                if (foundCauses == null)
                {
                    continue;
                }

                foreach (JObject entry in foundCauses)
                {
                    var category = GetCategory(entry);
                    if (category == null)
                    {
                        continue;
                    }

                    var description = entry.Value<string>("description");
                    var name = entry.Value<string>("name");
                    var cause = new BuildFailureCause(name: name, description: description, category: category);
                    causeList.Add(cause);
                    any = true;
                }
            }

            return any;
        }

        private static bool TryGetMergeConflict(JArray actions, List<BuildFailureCause> causeList)
        {
            foreach (var cur in actions)
            {
                var causes = (JArray)cur["causes"];
                if (causes == null)
                {
                    continue;
                }

                foreach (JObject obj in causes)
                {
                    var value = obj.Value<string>("shortDescription");
                    if (value != null && value.Contains("has merge conflicts"))
                    {
                        causeList.Add(BuildFailureCause.MergeConflict);
                        return true;
                    }
                }
            }

            return false;
        }

        private static string GetCategory(JObject causeItem)
        {
            var obj = causeItem["categories"];
            if (obj.Type == JTokenType.Null)
            {
                return null;
            }

            var items = (JArray)obj;
            if (items == null || items.Count == 0)
            {
                return null;
            }

            return items[0].Value<string>();
        }

        /// <summary>
        /// Convert to a unit test entry if this matches.
        /// </summary>
        private static bool TryGetUnitTestCauses(JArray actions, List<BuildFailureCause> causeList)
        {
            foreach (var cur in actions)
            {
                var data = cur as JObject;
                if (data == null)
                {
                    continue;
                }

                // The JSON looks like teh following:
                // {    "failCount" : 1,
                //      "skipCount" : 2546,
                //      "totalCount" : 66764,
                //      "urlName" : "testReport" }
                var urlName = data.Value<string>("urlName");
                var failCount = data.Value<int?>("failCount");
                if (string.IsNullOrEmpty(urlName) || !failCount.HasValue)
                {
                    continue;
                }

                var message = $"Unit Test Failure: {failCount}";
                causeList.Add(new BuildFailureCause("Unit Test", message, category: BuildFailureCause.CategoryTest));
                return true;
            }

            return false;
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
