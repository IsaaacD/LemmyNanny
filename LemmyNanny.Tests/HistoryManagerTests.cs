using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System.Data;
using System.IO;

namespace LemmyNanny.Tests
{
    [TestClass]
    public sealed class HistoryManagerTests
    {
        static string myDatabase = "file:cachedb?mode=memory&cache=shared";
        [ClassInitialize]
        public static void TestFixtureSetup(TestContext context)
        {

        }

        [ClassCleanup]
        public static void TestFixtureTearDown()
        {

        }

        [TestMethod]
        public void SetupDatabase_Creates_Database_and_Tables()
        {
            var historyManager = new HistoryManager(myDatabase);
            historyManager.SetupDatabase();
            var tables = new List<string>();
            using (var connection = new SqliteConnection($"DataSource={myDatabase}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY 1";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var value = reader.GetString(0);
                        tables.Add(value);
                    }
                }
            }

            Assert.AreEqual(3, tables.Count);
        }

        [TestMethod]
        public void HasRecord_Returns_False_When_No_Record()
        {
            var historyManager = new HistoryManager(myDatabase);
            historyManager.SetupDatabase();
            var hasRecord = historyManager.HasRecord(111, out _);
            Assert.IsFalse(hasRecord);
        }

        [TestMethod]
        public void HasRecord_Returns_True_When_Record()
        {
            var historyManager = new HistoryManager(myDatabase);
            historyManager.SetupDatabase();
            historyManager.AddRecord(new ProcessedPost { PostId=11, IsYes=true, Reason = "resason", Url="http://asdsd.com" });
            var hasRecord = historyManager.HasRecord(11, out _);
            Assert.IsTrue(hasRecord);
        }

        [TestMethod]
        public void AddRecord_Creates_Record()
        {
            var historyManager = new HistoryManager(myDatabase);
            historyManager.SetupDatabase();
            using (var connection = new SqliteConnection($"DataSource={myDatabase}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM seen_posts";
                using (var reader = command.ExecuteReader())
                {
                    var hasRows = reader.Read();
                    Assert.IsFalse(hasRows);
                }
            }
            historyManager.AddRecord(new ProcessedPost { IsYes = true, Reason="test", PostId = 1, Url = "fake.com" });
            using (var connection = new SqliteConnection($"DataSource={myDatabase}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM seen_posts";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var value = reader.GetInt32(0);
                        Assert.IsNotNull(value);
                    }
                }
            }
        }

        [TestMethod]
        public void AddRecord_Creates_Record_And_Increments_No()
        {
            var historyManager = new HistoryManager(myDatabase);
            historyManager.SetupDatabase();
            using (var connection = new SqliteConnection($"DataSource={myDatabase}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM seen_posts";
                using (var reader = command.ExecuteReader())
                {
                    var hasRows = reader.Read();
                    Assert.IsFalse(hasRows);
                }
            }
            historyManager.AddRecord(new ProcessedPost { IsYes = false, Reason = "test", PostId = 2, Url = "fake.com" });
            using (var connection = new SqliteConnection($"DataSource={myDatabase}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM stats";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var value = reader.GetInt32(1);
                        Assert.AreEqual(2, value);
                    }
                }
            }
        }

        [TestMethod]
        public void UpdateEndTime_Adds_Timestamp_To_End_Time()
        {
            var historyManager = new HistoryManager(myDatabase);
            historyManager.SetupDatabase();
            using (var connection = new SqliteConnection($"DataSource={myDatabase}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT end_time FROM stats";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var value = reader.IsDBNull(0);
                        Assert.IsTrue(value);
                    }
                }
            }
            historyManager.UpdateEndTime();

            using (var connection = new SqliteConnection($"DataSource={myDatabase}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT end_time FROM stats";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var value = reader.GetString(0);
                        Assert.IsNotNull(value);
                    }
                }
            }
        }
    }
}
