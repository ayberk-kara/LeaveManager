using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;
using System;
using System.Collections.Generic;

namespace LeaveManager.App.Services   // new namespace
{
    public class LeaveService
    {
        private readonly LeaveRepository _leaveRepository;
        private readonly EmployeeRepository _employeeRepository;
        private readonly List<LeaveRule> _rules;

        public LeaveService()
        {
            _leaveRepository = new LeaveRepository();
            _employeeRepository = new EmployeeRepository();

            _rules = new List<LeaveRule>
            {
                new DateRangeRule(),
                new NoPastStartRule(),
                //new MaxConsecutiveDaysRule(),
                new NoOverlapRule(),
                new LongLeaveGapRule(),
                new AnnualLeaveLimitRule(),
                new SickLeaveLimitRule()
            };
        }

        public bool TryAddLeave(Leave newLeave, out string errorMessage)
        {
            var employee = _employeeRepository.GetById(newLeave.EmployeeId);

            if (employee is null)
            {
                errorMessage = "Çalışan bulunamadı.";
                return false;
            }

            var existingLeaves = _leaveRepository.GetByEmployeeId(newLeave.EmployeeId);
            var allEmployees = _employeeRepository.GetAllActive();

            foreach (var rule in _rules)
            {
                if (!rule.Validate(employee, allEmployees, existingLeaves, newLeave, out errorMessage))
                    return false;
            }

            // Cross-year split
            var splitLeaves = SplitLeaveByYear(newLeave);

            foreach (var leavePart in splitLeaves)
            {
                EnsureBalanceExists(leavePart.EmployeeId, leavePart.Year);

                _leaveRepository.Add(leavePart);

                UpdateBalanceUsage(leavePart);
            }

            errorMessage = string.Empty;
            return true;
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

            // Last segment
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
        private void EnsureBalanceExists(int employeeId, int year)
        {
            var balance = _balanceRepository.GetByEmployeeAndYear(employeeId, year);

            if (balance != null)
                return;

            var prev = _balanceRepository.GetByEmployeeAndYear(employeeId, year - 1);

            int carry = 0;

            if (prev != null)
            {
                carry = prev.AnnualEntitled
                        + prev.AnnualManualAdjust
                        - prev.AnnualUsed;

                if (carry < 0)
                    carry = 0;
            }

            _balanceRepository.Create(new LeaveBalance
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

            // 2 yıl önceki bakiyeyi sil
            _balanceRepository.Delete(employeeId, year - 2);
        }

        // ------------------------------------------------------------
        // BALANCE UPDATE
        // ------------------------------------------------------------
        private void UpdateBalanceUsage(Leave leave)
        {
            var balance = _balanceRepository.GetByEmployeeAndYear(leave.EmployeeId, leave.Year);

            if (leave.Type == "Annual")
                balance.AnnualUsed += leave.Days;
            else if (leave.Type == "Sick")
                balance.SickUsed += leave.Days;

            _balanceRepository.Update(balance);
        }
    }
}