using Newtonsoft.Json;
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

        internal static List<string> GetTestCaseFailureList(JsonReader reader)
        {
            return GetTestCaseNames(reader, (name, status) =>
            {
                switch (status)
                {
                    case "PASSED":
                    case "SKIPPED":
                    case "FIXED":
                        return false;
                    case "FAILED":
                    case "REGRESSION":
                        return true;
                    default:
                        throw new Exception($"Unrecognized test case status {status}");
                }
            });
        }

        /// <summary>
        /// Get all of the test case names from the given stream.  A predicate can be supplied which takes the name and the 
        /// status of the test to filter out the results.
        /// </summary>
        /// <remarks>
        /// {
        ///   "testActions" : [
        ///   ],
        ///   "duration" : 1371.9198,
        ///   "empty" : false,
        ///   "failCount" : 0,
        ///   "passCount" : 175830,
        ///   "skipCount" : 178,
        ///   "suites" : [
        ///     {
        ///       "cases" : [
        /// </remarks>
        internal static List<string> GetTestCaseNames(JsonReader reader, Func<string, string, bool> statusFilter = null)
        {
            statusFilter = statusFilter ?? ((name, status) => true);

            var foundSuites = false;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == "suites")
                {
                    foundSuites = true;
                    break;
                }
            }

            var list = new List<string>();
            if (!foundSuites || !reader.Read() || reader.TokenType != JsonToken.StartArray)
            {
                return list;
            }

            ProcessSuite(reader, list, statusFilter);
            return list;
        }

        /// <summary>
        /// Parse out the objects in the case array.  The structure is 
        ///   {
        ///      "cases" : [
        ///        {
        ///          "testActions" : [ ]
        ///          "age" : 0,
        ///          "className" : "System.IO.Tests.FileInfo_Open_fm",
        ///          "duration" : 0.0014904,
        ///          "errorDetails" : null,
        ///          "errorStackTrace" : null,
        ///          "failedSince" : 0,
        ///          "name" : "FileModeAppendExisting",
        ///          "skipped" : false,
        ///          "skippedMessage" : null,
        ///          "status" : "PASSED",
        ///          "stderr" : null,
        ///          "stdout" : null
        ///        },
        /// </summary>
        private static void ProcessSuite(JsonReader reader, List<string> list, Func<string, string, bool> statusFilter)
        {
            Debug.Assert(reader.TokenType == JsonToken.StartArray);

            var foundCases = false;
            while (reader.Read())
            {
                if (reader.IsProperty("cases"))
                {
                    foundCases = true;
                    break;
                }
            }

            if (!foundCases || !reader.Read(JsonToken.StartArray))
            {
                return;
            }

            ProcessCasesArray(reader, list, statusFilter);
        }

        private static void ProcessCasesArray(JsonReader reader, List<string> list, Func<string, string, bool> statusFilter)
        {
            Debug.Assert(reader.TokenType == JsonToken.StartArray);

            reader.Read();
            while (reader.TokenType == JsonToken.StartObject)
            {
                ProcessCase(reader, list, statusFilter);
            }

            if (reader.TokenType == JsonToken.EndArray)
            {
                reader.Read();
            }
        }

        private static void ProcessCase(JsonReader reader, List<string> list, Func<string, string, bool> statusFilter)
        {
            Debug.Assert(reader.TokenType == JsonToken.StartObject);

            reader.Read();

            string className = null;
            string name = null;
            string status = null;
            do
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = (string)reader.Value;
                    switch (propertyName)
                    {
                        case "name":
                            name = reader.ReadAsString(); ;
                            break;
                        case "className":
                            className = reader.ReadAsString();
                            break;
                        case "status":
                            status = reader.ReadAsString();
                            break;
                        default:
                            reader.ReadProperty();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }

            } while (reader.TokenType != JsonToken.EndObject && reader.TokenType != JsonToken.None);

            if (className != null && name != null && status != null)
            {
                var fullName = $"{className}.{name}";
                if (statusFilter(fullName, status))
                {
                    list.Add(fullName);
                }
            }

            // Read the closing } for the case
            if (reader.TokenType == JsonToken.EndObject)
            {
                reader.Read();
            }
        }
    }
}
