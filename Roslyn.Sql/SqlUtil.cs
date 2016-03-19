using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Roslyn.Sql
{
    internal sealed class SqlUtil : IDisposable
    {
        internal const string Category = "sql";

        private SqlConnection _connection;
        private ILogger _logger;

        internal SqlUtil(string connectionString, ILogger logger = null)
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

        public int? GetTestRunCount()
        {
            var commandText = @"
                SELECT COUNT(*)
                FROM dbo.TestRuns";
            return RunCountCore(commandText);
        }

        internal int? GetStoreCount()
        {
            var commandText = @"
                SELECT COUNT(*)
                FROM dbo.TestResultStore";
            return RunCountCore(commandText);
        }

        /// <summary>
        /// Get the ID tracking the build source row for the unique pair of machine name and enlistment root.
        /// </summary>
        internal int? GetBuildSourceId(string machineName, string enlistmentRoot)
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

        private int? RunCountCore(string commandText)
        {
            using (var command = new SqlCommand(commandText, _connection))
            {
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
                    return null;
                }
            }
        }

        internal Tuple<int, int> GetStats()
        {
            var hitCount = GetStats(isHit: true);
            var missCount = GetStats(isHit: false);
            return Tuple.Create(hitCount ?? 0, missCount ?? 0);
        }

        private int? GetStats(bool isHit)
        {
            var commandText = @"
                SELECT COUNT(*)
                FROM dbo.TestResultQueries
                WHERE IsHit = @IsHit";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@IsHit", isHit);

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

        internal bool Insert(string checksum, string assemblyName, int outputStandardLength, int outputErrorLength, int contentLength, TimeSpan elapsed)
        {
            var commandText = @"
                INSERT INTO dbo.TestResultStore(Checksum, OutputStandardLength, OutputErrorLength, ContentLength, AssemblyName, ElapsedSeconds)
                VALUES(@Checksum, @OutputStandardLength, @OutputErrorLength, @ContentLength, @AssemblyName, @ElapsedSeconds)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Checksum", checksum);
                p.AddWithValue("@OutputStandardLength", outputStandardLength);
                p.AddWithValue("@OutputErrorLength", outputErrorLength);
                p.AddWithValue("@ContentLength", contentLength);
                p.AddWithValue("@AssemblyName", (object)assemblyName ?? DBNull.Value);
                p.AddWithValue("@ElapsedSeconds", elapsed.TotalSeconds);

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

        internal bool InsertHit(string checksum, string assemblyName, bool? isJenkins, int? buildSourceId)
        {
            return InsertTestQuery(checksum, assemblyName, isHit: true, isJenkins: isJenkins, buildSourceId: buildSourceId);
        }

        internal bool InsertMiss(string checksum, string assemblyName, bool? isJenkins, int? buildSourceId)
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

        internal bool InsertTestRun(TestRun testRun)
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

        internal bool InsertTestResult(string checksum, TestResult testResult)
        {
            var commandText = @"
                INSERT INTO dbo.TestResult(Checksum, ExitCode, OutputStandard, OutputError, ResultsFileContent, ResultsFileName, ElapsedSeconds, StoreDate)
                VALUES(@Checksum, @ExitCode, @OutputStandard, @OutputError, @ResultsFileContent, @ResultsFileName, @ElapsedSeconds, @StoreDate)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Checksum", checksum);
                p.AddWithValue("@ExitCode", testResult.ExitCode);
                p.AddWithValue("@OutputStandard", testResult.OutputStandard);
                p.AddWithValue("@OutputError", testResult.OutputError);
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

        internal TestResult? GetTestResult(string checksum)
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
                catch
                {
                    return null;
                }
            }
        }

        internal List<string> GetTestResultKeys()
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

        internal int? GetTestResultCount()
        {
            var commandText = @"
                SELECT COUNT(*)
                FROM dbo.TestResult";
            return RunCountCore(commandText);
        }

        internal bool ShaveTestResultTable()
        {
            var commandText = @"
                DELETE TOP(100)
                FROM dbo.TestResult
                ORDER BY StoreDate";
            using (var command = new SqlCommand(commandText, _connection))
            {
                try
                {
                    command.ExecuteNonQuery();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Log(Category, "Cannot shave test result table", ex);
                    return false;
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