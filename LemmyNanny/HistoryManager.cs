using dotNETLemmy.API.Types;
using Microsoft.Data.Sqlite;
using Spectre.Console;

namespace LemmyNanny
{
    public class HistoryManager : IHistoryManager
    {
        private readonly string _dbName;

        private string _connectionString => $"DataSource={_dbName}";

        public HistoryManager(string dbName)
        {
            _dbName = dbName;
            AnsiConsole.WriteLine($"{DateTime.Now}: Connection string = {_connectionString}");
        }

        public void SetupDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open(); // creates db
                var command = connection.CreateCommand();
                command.CommandText = @"CREATE TABLE IF NOT EXISTS ""seen_posts"" (
	                                ""post_id""	INTEGER UNIQUE,
	                                ""url""	TEXT,
	                                ""remarks""	TEXT,
	                                ""timestamp""	TEXT,
	                                PRIMARY KEY(""post_id"")
                                )";
                command.ExecuteNonQuery();

                command.CommandText = @"CREATE TABLE IF NOT EXISTS ""stats"" (
	                            ""yes_count""	INTEGER,
	                            ""no_count""	INTEGER,
	                            ""domain""	TEXT,
	                            ""start_time""	TEXT,
	                            ""end_time""	TEXT
                            )";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO stats (start_time, yes_count, no_count) VALUES ($startime, 0, 0)";
                command.Parameters.AddWithValue("$startime", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                command.ExecuteNonQuery();
            }
        }

        public void UpdateEndTime()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE stats SET end_time = $end_time";
                command.Parameters.AddWithValue("$end_time", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                command.ExecuteNonQuery();
            }
        }

        private int IncrementYesCount()
        {
            var value = 0;
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE stats SET yes_count = yes_count + 1;";
                command.ExecuteNonQuery();

                command.CommandText = "SELECT yes_count FROM stats;";
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        value = reader.GetInt32(0);
                        AnsiConsole.WriteLine($"{DateTime.Now}: Incremented yes_count to {value}");
                    }
                }
            }
            return value;
        }

        private int IncrementNoCount()
        {
            var value = 0;
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE stats SET no_count = no_count + 1;";
                command.ExecuteNonQuery();

                command.CommandText = "SELECT no_count FROM stats;";
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        value = reader.GetInt32(0);
                        AnsiConsole.WriteLine($"{DateTime.Now}: Incremented no_count to {value}");
                    }
                }
            }
            return value;
        }

        public void AddRecord(ProcessedPost post)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                        @$"
                            INSERT INTO seen_posts (post_id, url, remarks, timestamp)
                            VALUES ($id, $url, $remarks, $timestamp);
                        ";
                command.Parameters.AddWithValue("$id", post.PostId);
                command.Parameters.AddWithValue("$url", post.Url);
                command.Parameters.AddWithValue("$remarks", post.Reason);
                command.Parameters.AddWithValue("$timestamp", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                command.ExecuteNonQuery();
            }
            AnsiConsole.WriteLine($"{DateTime.Now}: Wrote post {post.PostId} to db.");

            if (post.IsYes)
            {
                IncrementYesCount();
            }
            else
            {
                IncrementNoCount();
            }
        }

        public bool HasRecord(int id, out string timestamp)
        {
            var alreadySeen = false;
            timestamp = string.Empty;
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                            SELECT timestamp
                            FROM seen_posts
                            WHERE post_id = $id
                        ";
                command.Parameters.AddWithValue("$id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // exists
                        timestamp = reader.GetString(0);
                        AnsiConsole.WriteLine($"{DateTime.Now}: Already seen post {id} @ {timestamp}. Skipping to next one.");
                        alreadySeen = true;
                    }
                }
            }
            return alreadySeen;
        }
    }

    public class ProcessedPost
    {
        public int PostId { get; set; }
        public string Url { get; set; }
        public string? Reason { get; set; }

        public bool IsYes { get; set; }
    }
}
