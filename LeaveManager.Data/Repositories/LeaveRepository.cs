using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using LeaveManager.Data.Models;
using LeaveManager.Data.Storage;

namespace LeaveManager.Data.Repositories
{
    public sealed class LeaveRepository
    {
        public void Add(Leave leave)
        {
            using var connection = new SqliteConnection(
                $"Data Source={DbPaths.GetDbFilePath()}");

            connection.Open();

            // ✅ Safety: ensure schema has EmployeeId column
            EnsureEmployeeIdColumnExists(connection);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Leaves (EmployeeId, StartDate, EndDate, Type, CreatedAt)
VALUES (@employeeId, @startDate, @endDate, @type, @createdAt);
";

            cmd.Parameters.AddWithValue("@employeeId", leave.EmployeeId);
            cmd.Parameters.AddWithValue("@startDate", leave.StartDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@endDate", leave.EndDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@type", leave.Type);
            cmd.Parameters.AddWithValue("@createdAt", leave.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            cmd.ExecuteNonQuery();
        }

        public List<Leave> GetByEmployee(int employeeId)
        {
            var result = new List<Leave>();

            using var connection = new SqliteConnection(
                $"Data Source={DbPaths.GetDbFilePath()}");

            connection.Open();
            System.Diagnostics.Debug.WriteLine("USING DB PATH: " + DbPaths.GetDbFilePath());

            // ✅ Safety: ensure schema has EmployeeId column
            EnsureEmployeeIdColumnExists(connection);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT Id, EmployeeId, StartDate, EndDate, Type, CreatedAt
FROM Leaves
WHERE EmployeeId = @employeeId
ORDER BY StartDate;
";

            cmd.Parameters.AddWithValue("@employeeId", employeeId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new Leave
                {
                    Id = reader.GetInt32(0),
                    EmployeeId = reader.GetInt32(1),
                    StartDate = DateTime.Parse(reader.GetString(2)),
                    EndDate = DateTime.Parse(reader.GetString(3)),
                    Type = reader.GetString(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5))
                });
            }

            return result;
        }

        private static void EnsureEmployeeIdColumnExists(SqliteConnection connection)
        {
            // 1) Leaves table exists? (if not, create it in correct shape)
            using (var createCmd = connection.CreateCommand())
            {
                createCmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Leaves (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    EmployeeId INTEGER NOT NULL,
    StartDate  TEXT    NOT NULL,
    EndDate    TEXT    NOT NULL,
    Type       TEXT    NOT NULL,
    CreatedAt  TEXT    NOT NULL
);
";
                createCmd.ExecuteNonQuery();
            }

            // 2) Check column existence
            var hasEmployeeId = false;
            using (var infoCmd = connection.CreateCommand())
            {
                infoCmd.CommandText = @"PRAGMA table_info(Leaves);";
                using var reader = infoCmd.ExecuteReader();
                while (reader.Read())
                {
                    var colName = reader.GetString(1);
                    if (string.Equals(colName, "EmployeeId", StringComparison.OrdinalIgnoreCase))
                    {
                        hasEmployeeId = true;
                        break;
                    }
                }
            }

            // 3) Add column if missing
            if (!hasEmployeeId)
            {
                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = @"
ALTER TABLE Leaves ADD COLUMN EmployeeId INTEGER NOT NULL DEFAULT 0;
";
                alterCmd.ExecuteNonQuery();
            }

            // 4) Index (safe to re-run)
            using (var idxCmd = connection.CreateCommand())
            {
                idxCmd.CommandText = @"
CREATE INDEX IF NOT EXISTS IX_Leaves_EmployeeId ON Leaves(EmployeeId);
CREATE INDEX IF NOT EXISTS IX_Leaves_StartDate  ON Leaves(StartDate);
";
                idxCmd.ExecuteNonQuery();
            }
        }
    }
}
