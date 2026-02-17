using LeaveManager.Data.Models;
using Microsoft.Data.Sqlite;

namespace LeaveManager.Data.Repositories
{
    public class LeaveBalanceRepository
    {
        public LeaveBalance? GetByEmployeeAndYear(
            SqliteConnection connection,
            int employeeId,
            int year)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT *
FROM LeaveBalances
WHERE employee_id = $emp AND year = $year;
";
            cmd.Parameters.AddWithValue("$emp", employeeId);
            cmd.Parameters.AddWithValue("$year", year);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return Map(reader);
        }

        public void Create(
            SqliteConnection connection,
            SqliteTransaction tx,
            LeaveBalance balance)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;

            cmd.CommandText = @"
INSERT INTO LeaveBalances
(employee_id, year,
 annual_entitled, annual_used, annual_manual_adjust,
 sick_entitled, sick_used, sick_manual_adjust)
VALUES
($emp, $year,
 $aEnt, $aUsed, $aAdj,
 $sEnt, $sUsed, $sAdj);
";

            cmd.Parameters.AddWithValue("$emp", balance.EmployeeId);
            cmd.Parameters.AddWithValue("$year", balance.Year);

            cmd.Parameters.AddWithValue("$aEnt", balance.AnnualEntitled);
            cmd.Parameters.AddWithValue("$aUsed", balance.AnnualUsed);
            cmd.Parameters.AddWithValue("$aAdj", balance.AnnualManualAdjust);

            cmd.Parameters.AddWithValue("$sEnt", balance.SickEntitled);
            cmd.Parameters.AddWithValue("$sUsed", balance.SickUsed);
            cmd.Parameters.AddWithValue("$sAdj", balance.SickManualAdjust);

            cmd.ExecuteNonQuery();
        }

        public void Update(
            SqliteConnection connection,
            SqliteTransaction tx,
            LeaveBalance balance)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;

            cmd.CommandText = @"
UPDATE LeaveBalances SET
 annual_entitled = $aEnt,
 annual_used = $aUsed,
 annual_manual_adjust = $aAdj,
 sick_entitled = $sEnt,
 sick_used = $sUsed,
 sick_manual_adjust = $sAdj
WHERE employee_id = $emp AND year = $year;
";

            cmd.Parameters.AddWithValue("$emp", balance.EmployeeId);
            cmd.Parameters.AddWithValue("$year", balance.Year);

            cmd.Parameters.AddWithValue("$aEnt", balance.AnnualEntitled);
            cmd.Parameters.AddWithValue("$aUsed", balance.AnnualUsed);
            cmd.Parameters.AddWithValue("$aAdj", balance.AnnualManualAdjust);

            cmd.Parameters.AddWithValue("$sEnt", balance.SickEntitled);
            cmd.Parameters.AddWithValue("$sUsed", balance.SickUsed);
            cmd.Parameters.AddWithValue("$sAdj", balance.SickManualAdjust);

            cmd.ExecuteNonQuery();
        }

        public void Delete(
            SqliteConnection connection,
            SqliteTransaction tx,
            int employeeId,
            int year)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;

            cmd.CommandText = @"
DELETE FROM LeaveBalances
WHERE employee_id = $emp AND year = $year;
";

            cmd.Parameters.AddWithValue("$emp", employeeId);
            cmd.Parameters.AddWithValue("$year", year);

            cmd.ExecuteNonQuery();
        }

        private LeaveBalance Map(SqliteDataReader reader)
        {
            return new LeaveBalance
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
                Year = reader.GetInt32(reader.GetOrdinal("year")),

                AnnualEntitled = reader.GetInt32(reader.GetOrdinal("annual_entitled")),
                AnnualUsed = reader.GetInt32(reader.GetOrdinal("annual_used")),
                AnnualManualAdjust = reader.GetInt32(reader.GetOrdinal("annual_manual_adjust")),

                SickEntitled = reader.GetInt32(reader.GetOrdinal("sick_entitled")),
                SickUsed = reader.GetInt32(reader.GetOrdinal("sick_used")),
                SickManualAdjust = reader.GetInt32(reader.GetOrdinal("sick_manual_adjust"))
            };
        }
    }
}