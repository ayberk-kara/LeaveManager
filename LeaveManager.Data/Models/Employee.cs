using LeaveManager.Data.Models;
using System;
using System.Collections.Generic;

namespace LeaveManager.Models
{
    public enum EmployeeRole
    {
        Assistant = 1,
        Employee = 2
    }

    public class Employee
    {
        public int Id { get; set; }

        public int SicilNo { get; set; }

        public string FullName { get; set; } = string.Empty;

        public EmployeeRole Role { get; set; }

        public int? ManagerId { get; set; }

        public bool IsActive { get; set; } = true;

        // ---- Leave Tracking ----

        // Current total annual leave balance (after carry + usage)
        public int AnnualLeaveBalance { get; set; }

        // Year -> Remaining leave from that specific year (2-year expiration logic)
        public Dictionary<int, int> YearlyLeaveBuckets { get; set; } = new();

        // Sick leave used in current calendar year (max 40 rule)
        public int CurrentYearSickLeaveUsed { get; set; }

        // Navigation for all leaves (for overlap & yearly calculations)
        public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
    }
}