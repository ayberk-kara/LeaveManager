using Microsoft.Data.Sqlite;

namespace LeaveManager.Data.Migrations
{
    public sealed class Migration_003_CreateLeavesTable : IMigration
    {
        public int Version => 3;

        public void Up(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();

            // Minimal leave storage for Sprint 1 (persist + list)
            // Dates are stored as ISO text (yyyy-MM-dd) for SQLite friendliness.
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Leaves (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    EmployeeId INTEGER NOT NULL,
    StartDate  TEXT    NOT NULL,
    EndDate    TEXT    NOT NULL,
    Type       TEXT    NOT NULL,
    CreatedAt  TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Leaves_EmployeeId ON Leaves(EmployeeId);
CREATE INDEX IF NOT EXISTS IX_Leaves_StartDate  ON Leaves(StartDate);
";
            cmd.ExecuteNonQuery();
        }
    }
}
