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
        private SqlConnection _connection;

        internal SqlUtil(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
            _connection.Open();
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }

        internal int? GetStoreCount()
        {
            var commandText = @"
                SELECT COUNT(*)
                FROM dbo.TestResultStore";
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
                    return 0;
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

        internal bool Insert(string checksum, string assemblyName, int outputStandardLength, int outputErrorLength, int contentLength)
        {
            var commandText = @"
                INSERT INTO dbo.TestResultStore(Checksum, OutputStandardLength, OutputErrorLength, ContentLength, AssemblyName)
                VALUES(@Checksum, @OutputStandardLength, @OutputErrorLength, @ContentLength, @AssemblyName)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Checksum", checksum);
                p.AddWithValue("@OutputStandardLength", outputStandardLength);
                p.AddWithValue("@OutputErrorLength", outputErrorLength);
                p.AddWithValue("@ContentLength", contentLength);
                p.AddWithValue("@AssemblyName", (object)assemblyName ?? DBNull.Value);

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

        internal bool InsertHit(string checksum, string assemblyName, bool? isJenkins)
        {
            return InsertQuery(checksum, assemblyName, isHit: true, isJenkins: isJenkins);
        }

        internal bool InsertMiss(string checksum, string assemblyName, bool? isJenkins)
        {
            return InsertQuery(checksum, assemblyName, isHit: false, isJenkins: isJenkins);
        }

        private bool InsertQuery(string checksum, string assemblyName, bool isHit, bool? isJenkins)
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
                catch 
                {
                    return false;
                }
            }
        }
    }
}