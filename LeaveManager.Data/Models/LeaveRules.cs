using LeaveManager.Data.Models;
using LeaveManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LeaveManager.App
{
    public abstract class LeaveRule
    {
        public string RuleName { get; }

        protected LeaveRule(string ruleName)
        {
            RuleName = ruleName;
        }

        public abstract bool Validate(
            Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason);
    }

    // ================= 1 =================
    // End date must be >= Start date
    public class DateRangeRule : LeaveRule
    {
        public DateRangeRule() : base("Invalid Date Range") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (newLeave.EndDate < newLeave.StartDate)
            {
                reason = "Leave end date must be greater than or equal to start date.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    // ================= 2 =================
    // Leave cannot start in the past
    public class NoPastStartRule : LeaveRule
    {
        public NoPastStartRule() : base("No Past Start Rule") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (newLeave.StartDate.Date < DateTime.Today)
            {
                reason = "Leave cannot start in the past.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    // ================= 3 =================
    // Maximum 10 consecutive days
    public class MaxConsecutiveDaysRule : LeaveRule
    {
        public MaxConsecutiveDaysRule() : base("Maximum Consecutive Leave Rule") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            int duration = (newLeave.EndDate - newLeave.StartDate).Days + 1;

            if (duration > 10)
            {
                reason = "Maximum consecutive leave duration is 10 days.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    // ================= 4 =================
    // Overlap rule
    public class NoOverlapRule : LeaveRule
    {
        public NoOverlapRule() : base("No Overlapping Leave Rule") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (existingLeaves.Any(l =>
                newLeave.StartDate <= l.EndDate &&
                newLeave.EndDate >= l.StartDate))
            {
                reason = "Leave dates overlap with an existing leave.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    // ================= 5 =================
    // Only one leave per same start date
    public class OneLeavePerDayRule : LeaveRule
    {
        public OneLeavePerDayRule() : base("Single Leave Per Start Date Rule") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (existingLeaves.Any(l => l.StartDate.Date == newLeave.StartDate.Date))
            {
                reason = "Only one leave request per start date is allowed.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    // ================= 6 =================
    // Sick leave <= 40 per year
    public class SickLeaveLimitRule : LeaveRule
    {
        public SickLeaveLimitRule() : base("Sick Leave Yearly Limit Rule") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (newLeave.Type != "Sick")
            {
                reason = string.Empty;
                return true;
            }

            int year = newLeave.StartDate.Year;

            int total = existingLeaves
                .Where(l => l.Type == "Sick" && l.StartDate.Year == year)
                .Sum(l => (l.EndDate - l.StartDate).Days + 1);

            total += (newLeave.EndDate - newLeave.StartDate).Days + 1;

            if (total > 40)
            {
                reason = "Sick leave cannot exceed 40 days per calendar year.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    // ================= 7 =================
    // Annual leave <= 30 per year
    public class AnnualLeaveLimitRule : LeaveRule
    {
        public AnnualLeaveLimitRule() : base("Annual Leave Yearly Limit Rule") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (newLeave.Type != "Annual")
            {
                reason = string.Empty;
                return true;
            }

            int year = newLeave.StartDate.Year;

            int total = existingLeaves
                .Where(l => l.Type == "Annual" && l.StartDate.Year == year)
                .Sum(l => (l.EndDate - l.StartDate).Days + 1);

            total += (newLeave.EndDate - newLeave.StartDate).Days + 1;

            if (total > 30)
            {
                reason = "Annual leave cannot exceed 30 days per calendar year.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    // ================= 8 =================
    // Same assistant cannot have overlapping annual leaves
    public class AssistantConflictRule : LeaveRule
    {
        public AssistantConflictRule() : base("Assistant Conflict Rule") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (newLeave.Type != "Annual" || employee.ManagerId == null)
            {
                reason = string.Empty;
                return true;
            }

            var sameAssistantEmployees = allEmployees
                .Where(e => e.ManagerId == employee.ManagerId && e.Id != employee.Id);

            foreach (var other in sameAssistantEmployees)
            {
                foreach (var leave in other.Leaves.Where(l => l.Type == "Annual"))
                {
                    if (newLeave.StartDate <= leave.EndDate &&
                        newLeave.EndDate >= leave.StartDate)
                    {
                        reason = "Two employees under the same assistant manager cannot take annual leave at the same time.";
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }
    }

    // ================= RULE MANAGER =================

    public static class LeaveRules
    {
        private static readonly List<LeaveRule> _rules = new()
        {
            new DateRangeRule(),
            new NoPastStartRule(),
            new MaxConsecutiveDaysRule(),
            new NoOverlapRule(),
            new OneLeavePerDayRule(),
            new SickLeaveLimitRule(),
            new AnnualLeaveLimitRule(),
            new AssistantConflictRule()
        };

        public static bool ValidateAll(
            Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            foreach (var rule in _rules)
            {
                if (!rule.Validate(employee, allEmployees, existingLeaves, newLeave, out reason))
                    return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}