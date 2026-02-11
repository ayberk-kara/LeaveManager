using Microsoft.Data.Sqlite;

namespace LeaveManager.Data.Migrations
{
    public sealed class Migration_001_Init : IMigration
    {
        public int Version => 1;

        public void Up(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();

            cmd.CommandText = @"

CREATE TABLE IF NOT EXISTS Employees (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    sicil_no INTEGER NOT NULL UNIQUE,
    full_name TEXT NOT NULL,
    role INTEGER NOT NULL, -- 0 employee, 1 assistant, 2 director
    manager_id INTEGER NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_utc TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ','now')),
    FOREIGN KEY(manager_id) REFERENCES Employees(id)
);

CREATE TABLE IF NOT EXISTS Leaves (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    employee_id INTEGER NOT NULL,
    start_date TEXT NOT NULL,
    end_date TEXT NOT NULL,
    type TEXT NOT NULL,
    note TEXT NULL,
    created_utc TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ','now')),
    FOREIGN KEY(employee_id) REFERENCES Employees(id)
);

";
            cmd.ExecuteNonQuery();
        }
    }
}