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
                new MaxConsecutiveDaysRule(),
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

            _leaveRepository.Add(newLeave);

            errorMessage = string.Empty;
            return true;
        }
    }
}