using Microsoft.Data.Sqlite;

namespace LeaveManager.Data.Migrations
{
    public sealed class Migration_005_AddEmployeeManagerAssignments : IMigration
    {
        public int Version => 5;

        public void Up(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();

            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS EmployeeManagerAssignments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EmployeeId INTEGER NOT NULL,
    ManagerId INTEGER NOT NULL,
    StartDate TEXT NOT NULL,
    EndDate TEXT NOT NULL,
    FOREIGN KEY(EmployeeId) REFERENCES Employees(Id),
    FOREIGN KEY(ManagerId) REFERENCES Employees(Id)
);";

            cmd.ExecuteNonQuery();
        }
    }
}