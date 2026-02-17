using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using LeaveManager.Data.Models;

namespace LeaveManager.Data.Repositories
{
    public sealed class LeaveRepository
    {
        public void Add(SqliteConnection connection, SqliteTransaction tx, Leave leave)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;

            cmd.CommandText = @"
INSERT INTO Leaves 
(employee_id, start_date, end_date, days, type, created_utc)
VALUES 
(@employeeId, @startDate, @endDate, @days, @type, @createdUtc);
";

            cmd.Parameters.AddWithValue("@employeeId", leave.EmployeeId);
            cmd.Parameters.AddWithValue("@startDate", leave.StartDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@endDate", leave.EndDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@days", leave.Days);
            cmd.Parameters.AddWithValue("@type", leave.Type);
            cmd.Parameters.AddWithValue("@createdUtc", leave.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            cmd.ExecuteNonQuery();
        }

        public List<Leave> GetByEmployeeId(SqliteConnection connection, int employeeId)
        {
            var result = new List<Leave>();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT id, employee_id, start_date, end_date, type, created_utc
FROM Leaves
WHERE employee_id = @employeeId
ORDER BY start_date;
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
    }
}