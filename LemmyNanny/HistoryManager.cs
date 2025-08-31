using dotNETLemmy.API.Types;
using Microsoft.Data.Sqlite;
using Spectre.Console;

namespace LemmyNanny
{
    public class HistoryManager
    {
        private readonly string _dbConnection;

        public HistoryManager(string dbConnection) 
        { 
            _dbConnection = dbConnection;
        }

        public void AddRecord(ProcessedPost post)
        {
            using (var connection = new SqliteConnection(_dbConnection))
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
            AnsiConsole.WriteLine($"Wrote post {post.PostId} to db.");
        }

        public bool HasRecord(int id, out string timestamp)
        {
            var alreadySeen = false;
            timestamp = string.Empty;
            using (var connection = new SqliteConnection(_dbConnection))
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
                        AnsiConsole.WriteLine($"Already seen post {id} @ {timestamp}. Skipping to next one.");
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
    }
}
