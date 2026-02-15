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

        /// <summary>
        /// Unique personnel number. Numeric only. Immutable after creation.
        /// </summary>
        public int SicilNo { get; set; }

        public string FullName { get; set; } = string.Empty;

        public EmployeeRole Role { get; set; }

        /// <summary>
        /// Reporting hierarchy:
        /// Assistant  -> must report to Manager
        /// Employee   -> must report to Assistant
        /// </summary>
        public int? ManagerId { get; set; }

        /// <summary>
        /// Soft delete flag.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}