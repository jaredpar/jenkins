using Dashboard.Azure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Dashboard.Sql
{
    public sealed class SqlUtil : IDisposable
    {
        public const string Category = "sql";
        public static DateTime DateTimeMin => new DateTime(year: 2016, month: 1, day: 1).ToUniversalTime();
        public static DateTime DateTimeMax => DateTime.UtcNow;

        private SqlConnection _connection;
        private ILogger _logger;

        public SqlUtil(string connectionString, ILogger logger = null)
        {
            _connection = new SqlConnection(connectionString);
            _connection.Open();
            _logger = logger ?? StorageLogger.Instance;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }

        public int? GetTestRunCount(DateTime? startDate = null)
        {
            var commandText = @"
                SELECT COUNT(*)
                FROM dbo.TestRuns";
            return RunCountCore(commandText, "RunDate", startDate);
        }

        public int? GetStoreCount(DateTime? startDate = null)
        {
            var commandText = @"
                SELECT COUNT(*)
                FROM dbo.TestResultStore";
            return RunCountCore(commandText, "StoreDate", startDate);
        }

        /// <summary>
        /// Get the ID tracking the build source row for the unique pair of machine name and enlistment root.
        /// </summary>
        public int? GetBuildSourceId(string machineName, string enlistmentRoot)
        {
            if (string.IsNullOrEmpty(machineName) || string.IsNullOrEmpty(enlistmentRoot))
            {
                return null;
            }

            machineName = machineName.ToLowerInvariant();
            enlistmentRoot = enlistmentRoot.ToLowerInvariant();

            // This is terrible use of SQL but for the moment it will suit my purpose. 
            var id = GetBuildSourceIdCore(machineName, enlistmentRoot);
            if (!id.HasValue)
            {
                InsertBuildSourceIdCore(machineName, enlistmentRoot);
                id = GetBuildSourceIdCore(machineName, enlistmentRoot);
            }

            return id;
        }

        private int? GetBuildSourceIdCore(string machineName, string enlistmentRoot)
        {
            var commandText = @"
                SELECT Id
                FROM dbo.BuildSource
                WHERE MachineName = @MachineName AND EnlistmentRoot = @EnlistmentRoot";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@MachineName", machineName);
                p.AddWithValue("@EnlistmentRoot", enlistmentRoot);

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetInt32(0);
                        }

                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        private bool InsertBuildSourceIdCore(string machineName, string enlistmentRoot)
        {
            var commandText = @"
                INSERT INTO dbo.BuildSource(MachineName, EnlistmentRoot)
                VALUES(@MachineName, @EnlistmentRoot)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@MachineName", machineName);
                p.AddWithValue("@EnlistmentRoot", enlistmentRoot);

                try
                {
                    command.ExecuteNonQuery();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private int? RunCountCore(string commandText, string dateFieldName, DateTime? startTime)
        {
            if (startTime.HasValue)
            {
                commandText += Environment.NewLine + $"WHERE {dateFieldName} >= @{dateFieldName}";
            }

            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                if (startTime.HasValue)
                {
                    p.AddWithValue($"@{dateFieldName}", startTime.Value.ToUniversalTime());
                }

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            return reader.GetInt32(0);
                        }

                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(Category, "Cannot get count", ex);
                    return null;
                }
            }
        }

        public List<DateTime> GetJenkinsTestRunDateTimes()
        {
            var commandText = @"
                SELECT RunDate
                FROM dbo.TestRuns
                WHERE IsJenkins = 1";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var list = new List<DateTime>();
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(reader.GetDateTime(0));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(Category, "Cannot get test run date times", ex);
                }

                return list;
            }
        }

        public List<TestRunLegacy> GetTestRuns(DateTime? startDateTime = null, DateTime? endDateTime = null)
        {
            startDateTime = startDateTime?.ToUniversalTime();
            endDateTime = endDateTime?.ToUniversalTime();

            var commandText = @"
                SELECT RunDate, Cache, ElapsedSeconds, Succeeded, IsJenkins, Is32, AssemblyCount, CacheCount, ChunkCount
                FROM dbo.TestRuns
                WHERE RunDate >= @StartDate AND RunDate <= @EndDate";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@StartDate", startDateTime ?? DateTimeMin);
                p.AddWithValue("@EndDate", endDateTime ?? DateTimeMax);
                var list = new List<TestRunLegacy>();
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var runDate = reader.GetDateTime(0).ToLocalTime();
                            var cache = reader.GetString(1);
                            var elapsed = reader.GetInt32(2);
                            var succeeded = reader.GetBoolean(3);
                            var isJenkins = reader.GetBoolean(4);
                            var is32Bit = reader.GetBoolean(5);
                            var assemblyCount = reader.GetInt32(6);
                            var cacheCount = reader.GetInt32(7);
                            var chunkCount = reader.GetInt32(8);

                            var testRun = new TestRunLegacy(
                                runDate: runDate,
                                cache: cache,
                                elapsed: TimeSpan.FromSeconds(elapsed),
                                succeeded: succeeded,
                                isJenkins: isJenkins,
                                is32Bit: is32Bit,
                                assemblyCount: assemblyCount,
                                cacheCount: cacheCount,
                                chunkCount: chunkCount);

                            list.Add(testRun);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(Category, "Cannot get test runs", ex);
                }

                return list;
            }
        }

        /// <summary>
        /// Get all of the statistics on test cache hits recorded in DB since the given <paramref name="startDate"/>.
        /// </summary>
        public TestHitStats? GetHitStats(DateTime? startDate)
        {
            var startDateValue = startDate ?? DateTimeMin;
            var commandText = @"
                SELECT COUNT(*), Sum(Passed), Sum(Failed), Sum(Skipped), Sum(ElapsedSeconds)
                FROM dbo.TestResultQueries
                INNER JOIN dbo.TestResultStore
                ON dbo.TestResultQueries.Checksum = dbo.TestResultStore.Checksum
                WHERE QueryDate >= @QueryDate AND IsHit = 1";

            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@QueryDate", startDateValue.ToUniversalTime());

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var assemblyCount = reader.GetInt32(0);
                            var passed = reader.GetInt32(1);
                            var failed = reader.GetInt32(2);
                            var skipped = reader.GetInt32(3);
                            var elapsed = reader.GetInt32(4);

                            return new TestHitStats()
                            {
                                AssemblyCount = assemblyCount,
                                TestsPassed = passed,
                                TestsFailed = failed,
                                TestsSkipped = skipped,
                                ElapsedSeconds = elapsed
                            };
                        }

                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(Category, "Unable to get hit stats", ex);
                    return null;
                }
            }
        }

        public int? GetMissStats(DateTime? startDate)
        {
            return GetStats(isHit: false, startDate: startDate);
        }

        private int? GetStats(bool isHit, DateTime? startDate)
        {
            var startDateValue = startDate ?? DateTime.MinValue;
            var commandText = @"
                SELECT COUNT(*)
                FROM dbo.TestResultQueries
                WHERE IsHit = @IsHit AND QueryDate >= @QueryDate";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@IsHit", isHit);
                p.AddWithValue("@QueryDate", startDateValue.ToUniversalTime());

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            return reader.GetInt32(0);
                        }

                        return null;
                    }
                }
                catch
                {
                    return 0;
                }
            }
        }

        public bool Insert(string checksum, string assemblyName, int outputStandardLength, int outputErrorLength, int contentLength, TestResultSummary summary, int? buildSourceId)
        {
            var commandText = @"
                INSERT INTO dbo.TestResultStore(Checksum, OutputStandardLength, OutputErrorLength, ContentLength, AssemblyName, Passed, Failed, Skipped, ElapsedSeconds, StoreDate, BuildSourceId)
                VALUES(@Checksum, @OutputStandardLength, @OutputErrorLength, @ContentLength, @AssemblyName, @Passed, @Failed, @Skipped, @ElapsedSeconds, @StoreDate, @BuildSourceId)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var buildSourceIdVal = buildSourceId.HasValue ? (object)buildSourceId.Value : DBNull.Value;
                var p = command.Parameters;
                p.AddWithValue("@Checksum", checksum);
                p.AddWithValue("@OutputStandardLength", outputStandardLength);
                p.AddWithValue("@OutputErrorLength", outputErrorLength);
                p.AddWithValue("@ContentLength", contentLength);
                p.AddWithValue("@AssemblyName", (object)assemblyName ?? DBNull.Value);
                p.AddWithValue("@Passed", summary.Passed);
                p.AddWithValue("@Failed", summary.Failed);
                p.AddWithValue("@Skipped", summary.Skipped);
                p.AddWithValue("@ElapsedSeconds", (int)summary.Elapsed.TotalSeconds);
                p.AddWithValue("@StoreDate", DateTime.UtcNow);
                p.AddWithValue("@BuildSourceId", buildSourceIdVal);

                try
                {
                    command.ExecuteNonQuery();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Log(Category, $"Cannot insert storage for {checksum}", ex);
                    return false;
                }
            }
        }

        public bool InsertHit(string checksum, string assemblyName, bool? isJenkins, int? buildSourceId)
        {
            return InsertTestQuery(checksum, assemblyName, isHit: true, isJenkins: isJenkins, buildSourceId: buildSourceId);
        }

        public bool InsertMiss(string checksum, string assemblyName, bool? isJenkins, int? buildSourceId)
        {
            return InsertTestQuery(checksum, assemblyName, isHit: false, isJenkins: isJenkins, buildSourceId: buildSourceId);
        }

        private bool InsertTestQuery(string checksum, string assemblyName, bool isHit, bool? isJenkins, int? buildSourceId)
        {
            var commandText = @"
                INSERT INTO dbo.TestResultQueries(Checksum, QueryDate, IsHit, IsJenkins, AssemblyName, BuildSourceId)
                VALUES(@Checksum, @QueryDate, @IsHit, @IsJenkins, @AssemblyName, @BuildSourceId)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var isJenkinsVal = isJenkins.HasValue ? (object)isJenkins.Value : DBNull.Value;
                var buildSourceIdVal = buildSourceId.HasValue ? (object)buildSourceId.Value : DBNull.Value;
                var p = command.Parameters;
                p.AddWithValue("@Checksum", checksum);
                p.AddWithValue("@QueryDate", DateTime.UtcNow);
                p.AddWithValue("@IsHit", isHit);
                p.AddWithValue("@IsJenkins", isJenkinsVal);
                p.AddWithValue("@AssemblyName", (object)assemblyName ?? DBNull.Value);
                p.AddWithValue("@BuildSourceId", buildSourceIdVal);

                try
                {
                    command.ExecuteNonQuery();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Log(Category, $"Cannot insert query for {checksum}", ex);
                    return false;
                }
            }
        }

        public bool InsertTestRun(TestRunLegacy testRun)
        {
            var commandText = @"
                INSERT INTO dbo.TestRuns(RunDate, Cache, ElapsedSeconds, Succeeded, IsJenkins, Is32, AssemblyCount, CacheCount, ChunkCount)
                VALUES(@RunDate, @Cache, @ElapsedSeconds, @Succeeded, @IsJenkins, @Is32, @AssemblyCount, @CacheCount, @ChunkCount)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@RunDate", testRun.RunDate);
                p.AddWithValue("@Cache", testRun.Cache);
                p.AddWithValue("@ElapsedSeconds", testRun.Elapsed.TotalSeconds);
                p.AddWithValue("@Succeeded", testRun.Succeeded);
                p.AddWithValue("@IsJenkins", testRun.IsJenkins);
                p.AddWithValue("@Is32", testRun.Is32Bit);
                p.AddWithValue("@AssemblyCount", testRun.AssemblyCount);
                p.AddWithValue("@CacheCount", testRun.CacheCount);
                p.AddWithValue("@ChunkCount", testRun.ChunkCount);

                try
                {
                    command.ExecuteNonQuery();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Log(Category, $"Cannot test run", ex);
                    return false;
                }
            }
        }

        public bool InsertTestResult(string checksum, TestResult testResult)
        {
            var commandText = @"
                INSERT INTO dbo.TestResult(Checksum, ExitCode, OutputStandard, OutputError, ResultsFileContent, ResultsFileName, ElapsedSeconds, StoreDate)
                VALUES(@Checksum, @ExitCode, @OutputStandard, @OutputError, @ResultsFileContent, @ResultsFileName, @ElapsedSeconds, @StoreDate)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Checksum", checksum);
                p.AddWithValue("@ExitCode", testResult.ExitCode);
                p.AddWithValue("@OutputStandard", (object)testResult.OutputStandard ?? DBNull.Value);
                p.AddWithValue("@OutputError", (object)testResult.OutputError ?? DBNull.Value);
                p.AddWithValue("@ResultsFileContent", ZipUtil.CompressText(testResult.ResultsFileContent));
                p.AddWithValue("@ResultsFileName", testResult.ResultsFileName);
                p.AddWithValue("@ElapsedSeconds", (int)testResult.Elapsed.TotalSeconds);
                p.AddWithValue("@StoreDate", DateTime.UtcNow);

                try
                {
                    command.ExecuteNonQuery();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Log(Category, "Cannot insert test result", ex);
                    return false;
                }
            }
        }

        public TestResult? GetTestResult(string checksum)
        {
            var commandText = @"
                SELECT ExitCode, OutputStandard, OutputError, ResultsFileContent, ResultsFileName, ElapsedSeconds
                FROM dbo.TestResult
                WHERE Checksum = @Checksum";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Checksum", checksum);

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var exitCode = reader.GetInt32(0);
                            var outputStandard = GetStringOrNull(reader, 1);
                            var outputError = GetStringOrNull(reader, 2);
                            var resultsFileContent = ZipUtil.DecompressText(GetAllBytes(reader, 3).ToArray());
                            var resultsFileName = reader.GetString(4);
                            var elapsed = reader.GetInt32(5);
                            return new TestResult(
                                exitCode: exitCode,
                                outputStandard: outputStandard,
                                outputError: outputError,
                                resultsFileName: resultsFileName,
                                resultsFileContent: resultsFileContent,
                                elapsed: TimeSpan.FromSeconds(elapsed));
                        }

                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(Category, $"Error getting {nameof(TestResult)}", ex);
                    return null;
                }
            }
        }

        public List<string> GetTestResultKeys()
        {
            var commandText = @"
                SELECT Checksum
                FROM dbo.TestResult";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var list = new List<string>();
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(reader.GetString(0));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(Category, "Error reading test result keys", ex);
                }

                return list;
            }
        }

        public int? GetTestResultCount(DateTime? startDate = null)
        {
            var commandText = @"
                SELECT COUNT(*)
                FROM dbo.TestResult";
            return RunCountCore(commandText, "StoreDate", startDate);
        }

        /// <summary>
        /// Clean the specified number of entries from the TestResult table which occured 
        /// past the specified date.
        /// </summary>
        public int? CleanTestResultTable(int count = 100, DateTime? storeDate = null)
        {
            var storeDateValue = storeDate?.ToUniversalTime() ?? DateTime.UtcNow - TimeSpan.FromDays(14);
            var commandText = @"
                DELETE TOP(@DeleteCount)
                FROM dbo.TestResult
                WHERE StoreDate <= @StoreDate";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@StoreDate", storeDateValue);
                p.AddWithValue("@DeleteCount", count);
                try
                {
                    return command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    _logger.Log(Category, "Cannot shave test result table", ex);
                    return null;
                }
            }
        }

        private static List<byte> GetAllBytes(SqlDataReader reader, int index)
        {
            var list = new List<byte>();

            var buffer = new byte[1000];
            var startIndex = 0;
            var read = (int)reader.GetBytes(index, startIndex, buffer, 0, buffer.Length);
            AddRange(list, buffer, read);

            while (read == buffer.Length)
            {
                startIndex += read;
                read = (int)reader.GetBytes(index, startIndex, buffer, 0, buffer.Length);
                AddRange(list, buffer, read);
            }

            return list;
        }

        private static string GetStringOrNull(SqlDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return null;
            }

            return reader.GetString(index);
        }

        private static void AddRange<T>(List<T> list, T[] buffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                list.Add(buffer[i]);
            }
        }
    }
}