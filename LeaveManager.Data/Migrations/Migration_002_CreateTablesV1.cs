using Microsoft.Data.Sqlite;

namespace LeaveManager.Data.Migrations
{
    public class Migration_002_CreateTablesV1 : IMigration
    {
        public int Version => 2;

        public void Up(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();

            command.CommandText = @"
PRAGMA foreign_keys = ON;

-- Groups (Departments/Teams)
CREATE TABLE IF NOT EXISTS Groups (
    id            INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    name          TEXT    NOT NULL,
    is_active     INTEGER NOT NULL DEFAULT 1,
    created_utc   TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Groups_Name ON Groups(name);

-- Employees
CREATE TABLE IF NOT EXISTS Employees (
    id             INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    full_name      TEXT    NOT NULL,
    group_id       INTEGER NULL,
    is_active      INTEGER NOT NULL DEFAULT 1,
    created_utc    TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),

    FOREIGN KEY (group_id) REFERENCES Groups(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS IX_Employees_GroupId ON Employees(group_id);
CREATE INDEX IF NOT EXISTS IX_Employees_FullName ON Employees(full_name);

-- Leaves
CREATE TABLE IF NOT EXISTS Leaves (
    id               INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    employee_id      INTEGER NOT NULL,
    start_date       TEXT    NOT NULL,  -- ISO date: YYYY-MM-DD
    end_date         TEXT    NOT NULL,  -- ISO date: YYYY-MM-DD
    days             INTEGER NOT NULL,  -- derived/validated later, v1 just stores
    type             TEXT    NOT NULL,  -- e.g., annual, unpaid, sick (v1: free text)
    note             TEXT    NULL,
    created_utc      TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),

    FOREIGN KEY (employee_id) REFERENCES Employees(id) ON DELETE CASCADE,

    CHECK (days > 0),
    CHECK (end_date >= start_date)
);

CREATE INDEX IF NOT EXISTS IX_Leaves_EmployeeId ON Leaves(employee_id);
CREATE INDEX IF NOT EXISTS IX_Leaves_StartDate ON Leaves(start_date);
CREATE INDEX IF NOT EXISTS IX_Leaves_EndDate ON Leaves(end_date);
";
            command.ExecuteNonQuery();
        }
    }
}
