using Microsoft.Data.Sqlite;

namespace LeaveManager.Data.Migrations
{
    public class Migration_001_Init : IMigration
    {
        public int Version => 1;

        public void Up(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();

            command.CommandText = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS AppMeta (
    key   TEXT NOT NULL PRIMARY KEY,
    value TEXT NOT NULL
);

INSERT INTO AppMeta(key, value)
SELECT 'schema_version', '1'
WHERE NOT EXISTS (
    SELECT 1 FROM AppMeta WHERE key = 'schema_version'
);
";
            command.ExecuteNonQuery();
        }
    }
}
