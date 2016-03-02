using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Roslyn.Sql
{
    public sealed class SqlUtil : IDisposable
    {
        private SqlConnection _connection;

        public SqlUtil(string connectionString)
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

        public bool Insert(string checksum, string assemblyName, int outputStandardLength, int outputErrorLength, int contentLength)
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
                p.AddWithValue("@AssemblyName", assemblyName);

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

        public bool 
    }
}