using Microsoft.Data.Sqlite;
using LeaveManager.Data.Migrations;
using LeaveManager.Data.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LeaveManager.Data
{
    public static class MigrationRunner
    {
        private static readonly List<IMigration> Migrations = new()
        {
            new Migration_001_Init(),
            new Migration_002_CreateTablesV1()
        };

        public static void RunMigrations()
        {
            var dbPath = DbPaths.GetDbFilePath();

            using var connection = CreateConnection(dbPath);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                var currentVersion = GetCurrentVersion(connection, transaction);

                var pendingMigrations = Migrations
                    .Where(m => m.Version > currentVersion)
                    .OrderBy(m => m.Version)
                    .ToList();

                foreach (var migration in pendingMigrations)
                {
                    migration.Up(connection); // Migration itself executes SQL
                    UpdateSchemaVersion(connection, transaction, migration.Version);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static int GetCurrentVersion(SqliteConnection connection, SqliteTransaction transaction)
        {
            // 1) Does AppMeta exist?
            using (var existsCmd = connection.CreateCommand())
            {
                existsCmd.Transaction = transaction;
                existsCmd.CommandText = @"
SELECT 1
FROM sqlite_master
WHERE type = 'table' AND name = 'AppMeta'
LIMIT 1;
";
                var exists = existsCmd.ExecuteScalar();
                if (exists == null)
                {
                    // No AppMeta table yet => DB is at version 0
                    return 0;
                }
            }

            // 2) AppMeta exists, try read schema_version
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"SELECT value FROM AppMeta WHERE key = 'schema_version' LIMIT 1;";
                var result = command.ExecuteScalar();

                if (result == null)
                    return 0;

                return int.Parse(result.ToString()!);
            }
        }

        private static void UpdateSchemaVersion(SqliteConnection connection, SqliteTransaction transaction, int version)
        {
            // AppMeta must exist by this point (Migration_001 creates it)
            using var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = @"
UPDATE AppMeta
SET value = $version
WHERE key = 'schema_version';
";
            command.Parameters.AddWithValue("$version", version.ToString());
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
