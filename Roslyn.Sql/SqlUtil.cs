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

        internal bool Insert(string checksum, string assemblyName, int outputStandardLength, int outputErrorLength, int contentLength, TimeSpan ellapsed)
        {
            var commandText = @"
                INSERT INTO dbo.TestResultStore(Checksum, OutputStandardLength, OutputErrorLength, ContentLength, AssemblyName, EllapsedSeconds)
                VALUES(@Checksum, @OutputStandardLength, @OutputErrorLength, @ContentLength, @AssemblyName, @EllapsedSeconds)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Checksum", checksum);
                p.AddWithValue("@OutputStandardLength", outputStandardLength);
                p.AddWithValue("@OutputErrorLength", outputErrorLength);
                p.AddWithValue("@ContentLength", contentLength);
                p.AddWithValue("@AssemblyName", (object)assemblyName ?? DBNull.Value);
                p.AddWithValue("@EllapsedSeconds", ellapsed.TotalSeconds);

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

        internal bool InsertHit(string checksum, string assemblyName, bool? isJenkins)
        {
            return InsertTestQuery(checksum, assemblyName, isHit: true, isJenkins: isJenkins);
        }

        internal bool InsertMiss(string checksum, string assemblyName, bool? isJenkins)
        {
            return InsertTestQuery(checksum, assemblyName, isHit: false, isJenkins: isJenkins);
        }

        private bool InsertTestQuery(string checksum, string assemblyName, bool isHit, bool? isJenkins)
        {
            var commandText = @"
                INSERT INTO dbo.TestResultQueries(Checksum, QueryDate, IsHit, IsJenkins, AssemblyName)
                VALUES(@Checksum, @QueryDate, @IsHit, @IsJenkins, @AssemblyName)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var isJenkinsVal = isJenkins.HasValue ? (object)isJenkins.Value : DBNull.Value;
                var p = command.Parameters;
                p.AddWithValue("@Checksum", checksum);
                p.AddWithValue("@QueryDate", DateTime.UtcNow);
                p.AddWithValue("@IsHit", isHit);
                p.AddWithValue("@IsJenkins", isJenkinsVal);
                p.AddWithValue("@AssemblyName", (object)assemblyName ?? DBNull.Value);

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
                INSERT INTO dbo.TestRuns(RunDate, Cache, EllapsedSeconds, Succeeded, IsJenkins, Is32, AssemblyCount, CacheCount, ChunkCount)
                VALUES(@RunDate, @Cache, @EllapsedSeconds, @Succeeded, @IsJenkins, @Is32, @AssemblyCount, @CacheCount, @ChunkCount)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@RunDate", testRun.RunDate);
                p.AddWithValue("@Cache", testRun.Cache);
                p.AddWithValue("@EllapsedSeconds", testRun.Ellapsed.TotalSeconds);
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
    }
}