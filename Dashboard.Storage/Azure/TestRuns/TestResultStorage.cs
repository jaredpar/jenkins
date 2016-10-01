using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;

namespace Dashboard.Azure.TestResults
{
    /// <summary>
    /// This is a proof of concept implementation only.  I realize that its implementation is pretty terrible
    /// and that's fine.  For now it is enough to validate the end to end scenario.
    /// </summary>
    public class TestResultStorage
    {
        internal sealed class TestResultJson
        {
            public int ExitCode { get; set; }
            public string OutputStandard { get; set; }
            public string OutputError { get; set; }
            public string ResultsFileName { get; set; }
            public string ResultsFileContent { get; set; }
            public double ElapsedSeconds { get; set; }
            public int Passed { get; set; }
            public int Failed { get; set; }
            public int Skipped { get; set; }
        }

        private const int SizeLimit = 10000000;

        public CloudBlobContainer TestResultsContainer { get; }
        public List<string> Keys => TestResultsContainer.ListBlobs().OfType<CloudBlockBlob>().Select(x => x.Name).ToList();
        public int Count => Keys.Count;

        public TestResultStorage(CloudStorageAccount account)
        {
            TestResultsContainer = account.CreateCloudBlobClient().GetContainerReference(AzureConstants.ContainerNames.TestResults);
        }

        public TestResultStorage(CloudBlobContainer testResultsContainer)
        {
            Debug.Assert(testResultsContainer.Name == AzureConstants.ContainerNames.TestResults);

            TestResultsContainer = testResultsContainer;
        }

        public void Add(string key, TestResult value)
        {
            if (string.IsNullOrEmpty(value.ResultsFileContent) || value.ResultsFileContent.Length > SizeLimit)
            {
                throw new Exception("Data too big");
            }

            var blob = TestResultsContainer.GetBlockBlobReference(key);

            var obj = new TestResultJson()
            {
                ExitCode = value.ExitCode,
                OutputStandard = value.OutputStandard,
                OutputError = value.OutputError,
                ResultsFileName = value.ResultsFileName,
                ResultsFileContent = value.ResultsFileContent,
                ElapsedSeconds = value.Elapsed.TotalSeconds,
                Passed = value.UnitTestData.Passed,
                Failed = value.UnitTestData.Failed,
                Skipped = value.UnitTestData.Skipped
            };

            var str = JsonConvert.SerializeObject(obj);
            blob.UploadText(str);
        }

        public bool TryGetValue(string key, out TestResult testResult)
        {
            var blob = TestResultsContainer.GetBlockBlobReference(key);
            if (!blob.Exists())
            {
                testResult = default(TestResult);
                return false;
            }

            var str = blob.DownloadText();
            var obj = (TestResultJson)JsonConvert.DeserializeObject(str, typeof(TestResultJson));
            testResult = new TestResult(
                exitCode: obj.ExitCode,
                outputStandard: obj.OutputStandard,
                outputError: obj.OutputError,
                resultsFileName: obj.ResultsFileName,
                resultsFileContent: obj.ResultsFileContent,
                unitTestData: new UnitTestData(
                    passed: obj.Passed,
                    failed: obj.Failed,
                    skipped: obj.Skipped),
                elapsed: TimeSpan.FromSeconds(obj.ElapsedSeconds));
            return true;
        }

        public int GetCount(DateTimeOffset? startDate)
        {
            var count = 0;
            foreach (var blob in TestResultsContainer.ListBlobs().OfType<CloudBlockBlob>())
            {
                var lastModified = blob.Properties.LastModified.Value.UtcDateTime;
                if (!startDate.HasValue || lastModified >= startDate.Value)
                {
                    count++;
                }
            }

            return count;
        }
    }
}