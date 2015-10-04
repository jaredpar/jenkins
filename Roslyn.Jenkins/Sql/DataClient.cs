using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Jenkins;

namespace Roslyn.Sql
{
    public sealed class DataClient : IDisposable
    {
        private SqlConnection _connection;

        public DataClient(string connectionString)
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

        public List<string> GetJobNames()
        {
            var commandText = @"SELECT DISTINCT Name FROM Jobs";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var list = new List<string>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(reader.GetString(0));
                    }
                }

                return list;
            }
        }

        /// <summary>
        /// Get the list of job names weighted by the number of runs they have.
        /// </summary>
        public List<string> GetJobNamesWeighted()
        {
            var commandText = @"
                SELECT Name
                FROM Jobs
                GROUP BY Name
                ORDER BY COUNT(Name) DESC";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var list = new List<string>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(reader.GetString(0));
                    }
                }

                return list;
            }
        }

        public TimeSpan GetAverageDuration(string jobName)
        {
            var commandText = @"
                SELECT AVG(Duration)
                FROM dbo.Jobs
                WHERE Name=@Name";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Name", jobName);

                var value = command.ExecuteScalar();
                var duration = (int)value;
                return TimeSpan.FromMilliseconds(duration);
            }
        }

        public List<Tuple<DateTime, TimeSpan>> GetDailyAverageDurations(string jobName)
        {
            var commandText = @"
                SELECT CAST ([Date] AS DATE), AVG(Duration)
                FROM dbo.Jobs
                WHERE Name=@Name
                GROUP BY CAST ([Date] AS DATE)
                ORDER BY CAST ([Date] AS DATE)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Name", jobName);

                var list = new List<Tuple<DateTime, TimeSpan>>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var date = reader.GetDateTime(0);
                        var duration = reader.GetInt32(1);
                        list.Add(Tuple.Create(date, TimeSpan.FromMilliseconds(duration)));
                    }
                }

                return list;
            }
        }

        /// <summary>
        /// Get the grouping of jobs (by day) which ended with the specified state.
        /// </summary>
        public List<Tuple<DateTime, int>> GetDailyJobCount(string jobName, JobState state)
        {
            var commandText = @"
                SELECT CAST ([Date] AS DATE), COUNT(State)
                FROM dbo.Jobs
                WHERE Name=@Name AND State=@State
                GROUP BY CAST ([Date] AS DATE)
                ORDER BY CAST ([Date] AS DATE)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Name", jobName);
                p.AddWithValue("@State", (int)state);

                var list = new List<Tuple<DateTime, int>>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var date = reader.GetDateTime(0);
                        var count = reader.GetInt32(1);
                        list.Add(Tuple.Create(date, count));
                    }
                }

                return list;
            }
        }

        public void InsertJobInfo(JobInfo jobInfo)
        {
            var id = jobInfo.Id;
            var commandText = @"
                INSERT INTO dbo.Jobs (Id, Name, Sha, State, Date, Duration)
                VALUES (@Id, @Name, @Sha, @State, @Date, @Duration)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Id", id.Id);
                p.AddWithValue("@Name", id.Name);
                p.AddWithValue("@Sha", jobInfo.Sha);
                p.AddWithValue("@State", (int)jobInfo.State);
                p.AddWithValue("@Date", jobInfo.Date);
                p.AddWithValue("@Duration", jobInfo.Duration.TotalMilliseconds);

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not insert {jobInfo}");
                    Console.WriteLine(ex.Message);
                }
            }
        }


        private static string GetKey(JobId id)
        {
            // TODO: implement
            throw new NotImplementedException();
        }

        public static JobId ParseKey(string key)
        {
            // TODO: implement
            throw new NotImplementedException();
        }

        public bool HasSucceeded(string name, string sha)
        {
            /*
            var commandText = @"
                SELECT Count(*)
                FROM Jobs
                WHERE Succeeded=1 AND Sha=@SHA AND JobKind=@JobKind";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@SHA", sha);
                p.AddWithValue("@JobKind", kind.ToString());

                var count = (int)command.ExecuteScalar();
                return count > 0;
            }
            */
            throw new NotImplementedException();
        }

        public int GetPullRequestId(JobId id)
        {
            var commandText = @"
                SELECT PullRequestId
                FROM Jobs
                WHERE Id=@Id";
            using (var command = new SqlCommand(commandText, _connection))
            {
                command.Parameters.AddWithValue("@Id", GetKey(id));

                var list = new List<Tuple<JobId, string>>();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new Exception("Missing data");
                    }

                    return reader.GetInt32(0);
                }
            }
        }

        public JobFailureInfo GetFailureInfo(JobId id)
        {
            var commandText = @"
                SELECT Reason,Messages 
                FROM Failures
                WHERE Id=@Id";
            using (var command = new SqlCommand(commandText, _connection))
            {
                command.Parameters.AddWithValue("@Id", GetKey(id));

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new Exception("Missing data");
                    }

                    var reason = reader.GetString(0);
                    var messages = reader.GetString(1).Split(';').ToList();
                    return new JobFailureInfo(
                        (JobFailureReason)(Enum.Parse(typeof(JobFailureReason), reason)),
                        messages);
                }
            }
        }

        public List<Tuple<JobId, string>> GetFailures()
        {
            var commandText = @"
                SELECT Id,Sha 
                FROM Failures";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var list = new List<Tuple<JobId, string>>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var key = reader.GetString(0);
                        var id = ParseKey(key);
                        var sha = reader.GetString(1);
                        list.Add(Tuple.Create(id, sha));
                    }
                }

                return list;
            }
        }

        public List<RetestInfo> GetRetestInfo()
        {
            var commandText = @"
                SELECT Id,Sha,Handled,Note
                FROM Retests";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var list = new List<RetestInfo>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var key = reader.GetString(0);
                        var sha = reader.GetString(1);
                        var handled = reader.GetBoolean(2);
                        var note = reader.IsDBNull(3)
                            ? null
                            : reader.GetString(3);
                        var info = new RetestInfo(
                            ParseKey(key),
                            sha,
                            handled,
                            note);
                        list.Add(info);
                    }
                }

                return list;
            }
        }

        public void InsertFailure(JobInfo info, JobFailureInfo failureInfo)
        {
            var commandText = @"
                INSERT INTO dbo.Failures (Id, Sha, Reason, Messages)
                VALUES (@Id, @Sha, @Reason, @Messages)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Id", GetKey(info.Id));
                p.AddWithValue("@Sha", info.Sha);
                p.AddWithValue("@Reason", failureInfo.Reason.ToString());
                p.AddWithValue("@Messages", string.Join(";", failureInfo.Messages));

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not insert failure {info.Id}");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void InsertRetest(JobId jobId, string sha)
        {
            var commandText = @"
                INSERT INTO dbo.Retests (Id, Sha, Handled)
                VALUES (@Id, @Sha, @Handled)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Id", GetKey(jobId));
                p.AddWithValue("@Sha", sha);
                p.AddWithValue("@Handled", 0);

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not insert retest {jobId}");
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
