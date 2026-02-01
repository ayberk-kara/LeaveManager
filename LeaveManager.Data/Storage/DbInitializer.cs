using Microsoft.Data.Sqlite;
using System.IO;

namespace LeaveManager.Data.Storage
{
    public static class DbInitializer
    {
        public static void EnsureDatabaseReady()
        {
            EnsureFolderExists();
            EnsureDatabaseFileExists();
            EnsureForeignKeysEnabled();
        }

        private static void EnsureFolderExists()
        {
            var folder = DbPaths.GetDbFolderPath();

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        private static void EnsureDatabaseFileExists()
        {
            var dbPath = DbPaths.GetDbFilePath();

            if (!File.Exists(dbPath))
            {
                using var connection = CreateConnection(dbPath);
                connection.Open();
            }
        }

        private static void EnsureForeignKeysEnabled()
        {
            var dbPath = DbPaths.GetDbFilePath();

            using var connection = CreateConnection(dbPath);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys = ON;";
            command.ExecuteNonQuery();
        }

        private static SqliteConnection CreateConnection(string dbPath)
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            return new SqliteConnection(builder.ToString());
        }
    }
}
