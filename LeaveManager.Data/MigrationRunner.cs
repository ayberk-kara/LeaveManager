using Microsoft.Data.Sqlite;
using LeaveManager.Data.Migrations;

namespace LeaveManager.Data
{
    public static class MigrationRunner
    {
        public static void Run(SqliteConnection connection)
        {
            EnsureMetaTable(connection);

            var currentVersion = GetCurrentVersion(connection);

            var migrations = new IMigration[]
            {
                new Migration_001_Init()
            };

            foreach (var migration in migrations.OrderBy(m => m.Version))
            {
                if (migration.Version > currentVersion)
                {
                    migration.Up(connection);
                    SetVersion(connection, migration.Version);
                }
            }
        }

        private static void EnsureMetaTable(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS AppMeta (
    version INTEGER NOT NULL
);";
            cmd.ExecuteNonQuery();

            // ensure single row exists
            cmd.CommandText = "SELECT COUNT(*) FROM AppMeta;";
            var count = Convert.ToInt32(cmd.ExecuteScalar());

            if (count == 0)
            {
                cmd.CommandText = "INSERT INTO AppMeta (version) VALUES (0);";
                cmd.ExecuteNonQuery();
            }
        }

        private static int GetCurrentVersion(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT version FROM AppMeta LIMIT 1;";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static void SetVersion(SqliteConnection connection, int version)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE AppMeta SET version = $version;";
            cmd.Parameters.AddWithValue("$version", version);
            cmd.ExecuteNonQuery();
        }
    }
}