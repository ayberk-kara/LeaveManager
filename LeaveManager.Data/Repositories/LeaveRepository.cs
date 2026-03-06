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

        public void Delete(SqliteConnection connection, SqliteTransaction tx, int leaveId)
        {
            Leave? leave = null;

            using (var getCmd = connection.CreateCommand())
            {
                getCmd.Transaction = tx;

                getCmd.CommandText = @"
SELECT employee_id, type, days, year
FROM Leaves
WHERE id = @id;
";

                getCmd.Parameters.AddWithValue("@id", leaveId);

                using var reader = getCmd.ExecuteReader();

                if (reader.Read())
                {
                    leave = new Leave
                    {
                        EmployeeId = reader.GetInt32(0),
                        Type = reader.GetString(1),
                        Days = reader.GetInt32(2),
                        Year = reader.GetInt32(3)
                    };
                }
            }

            if (leave == null)
                return;

            using (var balanceCmd = connection.CreateCommand())
            {
                balanceCmd.Transaction = tx;

                bool isAnnual =
                    leave.Type.Equals("Yıllık", StringComparison.OrdinalIgnoreCase) ||
                    leave.Type.Equals("Annual", StringComparison.OrdinalIgnoreCase);

                bool isSick =
                    leave.Type.Equals("Hastalık", StringComparison.OrdinalIgnoreCase) ||
                    leave.Type.Equals("Sick", StringComparison.OrdinalIgnoreCase);

                if (isAnnual)
                {
                    balanceCmd.CommandText = @"
UPDATE LeaveBalances
SET annual_used = annual_used - @days
WHERE employee_id = @empId AND year = @year;
";
                }
                else if (isSick)
                {
                    balanceCmd.CommandText = @"
UPDATE LeaveBalances
SET sick_used = sick_used - @days
WHERE employee_id = @empId AND year = @year;
";
                }

                balanceCmd.Parameters.AddWithValue("@days", leave.Days);
                balanceCmd.Parameters.AddWithValue("@empId", leave.EmployeeId);
                balanceCmd.Parameters.AddWithValue("@year", leave.Year);

                balanceCmd.ExecuteNonQuery();
            }

            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM Leaves WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", leaveId);
            cmd.ExecuteNonQuery();
        }

        public int CountAnnualLeavesUnderManager(
    SqliteConnection connection,
    int managerId,
    DateTime startDate,
    DateTime endDate)
{
    using var cmd = connection.CreateCommand();

    cmd.CommandText = @"
SELECT COUNT(DISTINCT l.employee_id)
FROM Leaves l
JOIN EmployeeManagerAssignments a 
    ON a.EmployeeId = l.employee_id
WHERE a.ManagerId = @managerId
AND l.type IN ('Annual','Yıllık')
AND date(l.start_date) <= date(@endDate)
AND date(l.end_date) >= date(@startDate)
AND date(a.StartDate) <= date(@endDate)
AND date(a.EndDate) >= date(@startDate);
";

    cmd.Parameters.AddWithValue("@managerId", managerId);
    cmd.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
    cmd.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));

    return Convert.ToInt32(cmd.ExecuteScalar());
}




        public List<Leave> GetByEmployeeId(SqliteConnection connection, int employeeId)
        {
            var result = new List<Leave>();

            using var cmd = connection.CreateCommand();

            cmd.CommandText = @"
SELECT id, employee_id, start_date, end_date, type, days, year, created_utc
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
                    Days = reader.GetInt32(5),
                    Year = reader.GetInt32(6),
                    CreatedAt = DateTime.Parse(reader.GetString(7))
                });
            }

            return result;
        }
    }
}