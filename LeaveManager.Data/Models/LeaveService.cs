using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;
using LeaveManager.Data.Storage;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LeaveManager.App.Services
{
    public class LeaveService
    {
        private readonly LeaveRepository _leaveRepository;
        private readonly EmployeeRepository _employeeRepository;
        private readonly LeaveBalanceRepository _balanceRepository;
        private readonly List<LeaveRule> _rules;

        private string ConnectionString =>
            $"Data Source={DbPaths.GetDbFilePath()}";

        public LeaveService()
        {
            _leaveRepository = new LeaveRepository();
            _employeeRepository = new EmployeeRepository();
            _balanceRepository = new LeaveBalanceRepository();

            _rules = new List<LeaveRule>
            {
                new DateRangeRule(),
                new NoOverlapRule(),
                new LongLeaveGapRule(),
                new ManagerAssignmentExistsRule()
            };
        }

        private bool IsAnnual(string type)
        {
            var t = type?.Trim();

            return t.Equals("Yıllık", StringComparison.OrdinalIgnoreCase) ||
                   t.Equals("Annual", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSick(string type)
        {
            var t = type?.Trim();

            return t.Equals("Hastalık", StringComparison.OrdinalIgnoreCase) ||
                   t.Equals("Sick", StringComparison.OrdinalIgnoreCase) ||
                   t.Equals("Rapor", StringComparison.OrdinalIgnoreCase);
        }

        public bool TryAddLeave(Leave newLeave, out string errorMessage)
        {
            newLeave.Year = newLeave.StartDate.Year;

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var tx = connection.BeginTransaction();

            try
            {
                var employee = _employeeRepository.GetById(newLeave.EmployeeId);

                if (employee is null)
                {
                    errorMessage = "Çalışan bulunamadı.";
                    tx.Rollback();
                    return false;
                }

                var existingLeaves =
                    _leaveRepository.GetByEmployeeId(connection, newLeave.EmployeeId);

                var allEmployees = _employeeRepository.GetAllActive();

                foreach (var rule in _rules)
                {
                    if (!rule.Validate(employee, allEmployees, existingLeaves, newLeave, out errorMessage))
                    {
                        tx.Rollback();
                        return false;
                    }
                }

                var splitLeaves = SplitLeaveByYear(newLeave);

                foreach (var leavePart in splitLeaves)
                {
                    EnsureBalanceExists(connection, tx, leavePart.EmployeeId, leavePart.Year);

                    var balance = _balanceRepository
                        .GetByEmployeeAndYear(connection, leavePart.EmployeeId, leavePart.Year);

                    if (balance == null)
                        throw new Exception("Balance bulunamadı.");

                    bool isAnnual = IsAnnual(leavePart.Type);
                    bool isSick = IsSick(leavePart.Type);

                    
                    if (isAnnual)
                    {
                        int remaining =
                            balance.AnnualEntitled +
                            balance.AnnualManualAdjust -
                            balance.AnnualUsed;

                        if (leavePart.Days > remaining)
                        {
                            errorMessage = $"Yetersiz yıllık izin bakiyesi. Kalan: {remaining}";
                            tx.Rollback();
                            return false;
                        }
                    }
                    else if (isSick)
                    {
                        int remaining =
                            balance.SickEntitled +
                            balance.SickManualAdjust -
                            balance.SickUsed;

                        if (leavePart.Days > remaining)
                        {
                            errorMessage = $"Yetersiz hastalık izni bakiyesi. Kalan: {remaining}";
                            tx.Rollback();
                            return false;
                        }
                    }

                    _leaveRepository.Add(connection, tx, leavePart);

                    
                    RecalculateBalance(connection, tx, leavePart.EmployeeId, leavePart.Year);
                }

                tx.Commit();
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                errorMessage = ex.Message;
                return false;
            }
        }

        
        private void RecalculateBalance(SqliteConnection connection,
                                        SqliteTransaction tx,
                                        int employeeId,
                                        int year)
        {
            var leaves = _leaveRepository.GetByEmployeeId(connection, employeeId)
                .Where(l => l.Year == year)
                .ToList();

            int annualUsed = leaves
                .Where(l => IsAnnual(l.Type))
                .Sum(l => l.Days);

            int sickUsed = leaves
                .Where(l => IsSick(l.Type))
                .Sum(l => l.Days);

            var balance = _balanceRepository
                .GetByEmployeeAndYear(connection, employeeId, year);

            if (balance == null)
                throw new Exception("Balance bulunamadı.");

            balance.AnnualUsed = annualUsed;
            balance.SickUsed = sickUsed;

            _balanceRepository.Update(connection, tx, balance);
        }

        private List<Leave> SplitLeaveByYear(Leave original)
        {
            var result = new List<Leave>();

            var currentStart = original.StartDate.Date;
            var end = original.EndDate.Date;

            while (currentStart.Year < end.Year)
            {
                var yearEnd = new DateTime(currentStart.Year, 12, 31);
                var days = (yearEnd - currentStart).Days + 1;

                result.Add(new Leave
                {
                    EmployeeId = original.EmployeeId,
                    StartDate = currentStart,
                    EndDate = yearEnd,
                    Type = original.Type,
                    Days = days,
                    Year = currentStart.Year,
                    CreatedAt = DateTime.UtcNow
                });

                currentStart = yearEnd.AddDays(1);
            }

            var finalDays = (end - currentStart).Days + 1;

            result.Add(new Leave
            {
                EmployeeId = original.EmployeeId,
                StartDate = currentStart,
                EndDate = end,
                Type = original.Type,
                Days = finalDays,
                Year = currentStart.Year,
                CreatedAt = DateTime.UtcNow
            });

            return result;
        }

        private void EnsureBalanceExists(SqliteConnection connection,
                                         SqliteTransaction tx,
                                         int employeeId,
                                         int year)
        {
            var balance = _balanceRepository
                .GetByEmployeeAndYear(connection, employeeId, year);

            if (balance != null)
                return;

            var prev = _balanceRepository
                .GetByEmployeeAndYear(connection, employeeId, year - 1);

            int carry = 0;

            if (prev != null)
            {
                carry = prev.AnnualEntitled
                        + prev.AnnualManualAdjust
                        - prev.AnnualUsed;

                if (carry < 0)
                    carry = 0;

                if (carry > 30)
                    carry = 30;
            }

            _balanceRepository.Create(connection, tx, new LeaveBalance
            {
                EmployeeId = employeeId,
                Year = year,
                AnnualEntitled = 30 + carry,
                AnnualUsed = 0,
                AnnualManualAdjust = 0,
                SickEntitled = 40,
                SickUsed = 0,
                SickManualAdjust = 0
            });

            _balanceRepository.Delete(connection, tx, employeeId, year - 2);
        }

        public void DeleteLeave(int leaveId)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var tx = connection.BeginTransaction();

            try
            {
                Leave leave;

                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
SELECT employee_id, type, start_date, end_date
FROM Leaves
WHERE id = $id;
";

                    cmd.Parameters.AddWithValue("$id", leaveId);

                    using var reader = cmd.ExecuteReader();

                    if (!reader.Read())
                        throw new Exception("Leave bulunamadı.");

                    var startDate = DateTime.Parse(reader.GetString(2));
                    var endDate = DateTime.Parse(reader.GetString(3));

                    leave = new Leave
                    {
                        EmployeeId = reader.GetInt32(0),
                        Type = reader.GetString(1),
                        StartDate = startDate,
                        EndDate = endDate,
                        Days = (endDate - startDate).Days + 1,
                        Year = startDate.Year
                    };
                }

                _leaveRepository.Delete(connection, tx, leaveId);


                RecalculateBalance(connection, tx, leave.EmployeeId, leave.Year);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}