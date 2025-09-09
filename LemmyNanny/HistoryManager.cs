using LemmyNanny.Interfaces;
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
                                    ""is_reported"" INTEGER,
	                                PRIMARY KEY(""post_id"")
                                )";
                command.ExecuteNonQuery();
                command.CommandText = @"CREATE TABLE IF NOT EXISTS ""seen_comments"" (
	                                ""comment_id""	INTEGER UNIQUE,
                                    ""post_id"" INTEGER,
	                                ""url""	TEXT,
	                                ""remarks""	TEXT,
	                                ""timestamp""	TEXT,
                                    ""is_reported"" INTEGER,
	                                PRIMARY KEY(""comment_id"")
                                )";
                command.ExecuteNonQuery();
            }
        }

        public void AddPostRecord(ProcessedPost post)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                        @$"
                            INSERT INTO seen_posts (post_id, url, remarks, timestamp, is_reported)
                            VALUES ($id, $url, $remarks, $timestamp, $reported);
                        ";
                command.Parameters.AddWithValue("$id", post.Id);
                command.Parameters.AddWithValue("$url", post.Url);
                command.Parameters.AddWithValue("$remarks", post.Reason);
                command.Parameters.AddWithValue("$timestamp", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                command.Parameters.AddWithValue("$reported", post.IsYes ? 1 : 0);
                command.ExecuteNonQuery();
            }
            AnsiConsole.WriteLine($"{DateTime.Now}: Wrote post {post.Id} to db.");

        }

        public bool HasPostRecord(int id, out string timestamp)
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

        public bool HasCommentRecord(int id, out string timestamp)
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
                            FROM seen_comments
                            WHERE comment_id = $id
                        ";
                command.Parameters.AddWithValue("$id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // exists
                        timestamp = reader.GetString(0);
                        AnsiConsole.WriteLine($"{DateTime.Now}: Already seen comment {id} @ {timestamp}. Skipping to next one.");
                        alreadySeen = true;
                    }
                }
            }
            return alreadySeen;
        }

        public void AddCommentRecord(ProcessedComment comment)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                        @$"
                            INSERT INTO seen_comments (comment_id, post_id, url, remarks, timestamp, is_reported)
                            VALUES ($commentid, $postid, $url, $remarks, $timestamp, $reported);
                        ";
                command.Parameters.AddWithValue("$commentid", comment.Id);
                command.Parameters.AddWithValue("$postid", comment.PostId);
                command.Parameters.AddWithValue("$url", comment.Url);
                command.Parameters.AddWithValue("$remarks", comment.Reason);
                command.Parameters.AddWithValue("$timestamp", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                command.Parameters.AddWithValue("$reported", comment.IsYes ? 1 : 0);
                command.ExecuteNonQuery();
            }
            AnsiConsole.WriteLine($"{DateTime.Now}: Wrote comment {comment.Id} to db.");
        }
    }
}
