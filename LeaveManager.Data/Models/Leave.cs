using System;

namespace LeaveManager.Data.Models
{
    public sealed class Leave
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        // Annual / Sick
        public string Type { get; set; } = string.Empty;

        
        public int Days { get; set; }

        // balance change of year
        public int Year { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}