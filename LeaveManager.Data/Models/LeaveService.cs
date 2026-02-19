using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;
using LeaveManager.Data.Storage;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

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
                new NoPastStartRule(),
                new NoOverlapRule(),
                new LongLeaveGapRule(),
               
            };
        }

        public bool TryAddLeave(Leave newLeave, out string errorMessage)
        {
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

                    if (leavePart.Type == "Annual")
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
                    else if (leavePart.Type == "Sick")
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
                    UpdateBalanceUsage(connection, tx, leavePart);
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

        // ------------------------------------------------------------
        // CROSS YEAR SPLIT
        // ------------------------------------------------------------
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

        // ------------------------------------------------------------
        // BALANCE CREATE IF MISSING
        // ------------------------------------------------------------
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

        // ------------------------------------------------------------
        // BALANCE UPDATE
        // ------------------------------------------------------------
        private void UpdateBalanceUsage(SqliteConnection connection,
                                        SqliteTransaction tx,
                                        Leave leave)
        {
            var balance = _balanceRepository
                .GetByEmployeeAndYear(connection, leave.EmployeeId, leave.Year);

            if (balance == null)
                throw new Exception("Balance bulunamadı.");

            if (leave.Type.Equals("Yıllık", StringComparison.OrdinalIgnoreCase)
    || leave.Type.Equals("Annual", StringComparison.OrdinalIgnoreCase))
            {
                balance.AnnualUsed += leave.Days;
            }
            else if (leave.Type.Equals("Hastalık", StringComparison.OrdinalIgnoreCase)
                     || leave.Type.Equals("Sick", StringComparison.OrdinalIgnoreCase))
            {
                balance.SickUsed += leave.Days;
            }

            _balanceRepository.Update(connection, tx, balance);
        }
    }
}