using System;

namespace LeaveManager.Data.Models
{
    public sealed class LeaveBalance
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public int Year { get; set; }

        // Annual
        public int AnnualEntitled { get; set; } = 30;
        public int AnnualUsed { get; set; } = 0;
        public int AnnualManualAdjust { get; set; } = 0;

        // Sick
        public int SickEntitled { get; set; } = 40;
        public int SickUsed { get; set; } = 0;
        public int SickManualAdjust { get; set; } = 0;

        // Computed helpers
        public int AnnualRemaining =>
            AnnualEntitled + AnnualManualAdjust - AnnualUsed;

        public int SickRemaining =>
            SickEntitled + SickManualAdjust - SickUsed;
    }
}