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

        // unique personnel number, numeric only, never changes
        public int SicilNo { get; set; }

        public string FullName { get; set; } = string.Empty;

        public EmployeeRole Role { get; set; }

        // reporting hierarchy:
        // manager -> null
        // assistant -> must report to manager
        // employee -> must report to assistant
        public int? ManagerId { get; set; }

        // soft delete flag
        public bool IsActive { get; set; } = true;
    }
}