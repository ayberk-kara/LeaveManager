using Microsoft.Data.Sqlite;

namespace LeaveManager.Data.Migrations
{
    public sealed class Migration_003_AddLeaveBalances : IMigration
    {
        public int Version => 3;

        public void Up(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();

            cmd.CommandText = @"

CREATE TABLE IF NOT EXISTS LeaveBalances (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    employee_id INTEGER NOT NULL,
    year INTEGER NOT NULL,

    annual_entitled INTEGER NOT NULL DEFAULT 30,
    annual_used INTEGER NOT NULL DEFAULT 0,
    annual_manual_adjust INTEGER NOT NULL DEFAULT 0,

    sick_entitled INTEGER NOT NULL DEFAULT 40,
    sick_used INTEGER NOT NULL DEFAULT 0,
    sick_manual_adjust INTEGER NOT NULL DEFAULT 0,

    UNIQUE(employee_id, year),
    FOREIGN KEY(employee_id) REFERENCES Employees(id) ON DELETE CASCADE
);

";
            cmd.ExecuteNonQuery();
        }
    }
}