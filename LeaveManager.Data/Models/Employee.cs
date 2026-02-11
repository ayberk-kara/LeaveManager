namespace LeaveManager.Models
{
    public enum EmployeeRole
    {
        Manager = 0,
        Assistant = 1,
        Employee = 2
    }

    public class Employee
    {
        public int Id { get; set; }

        // unique and never changes
        public int SicilNo { get; set; }

        public string FullName { get; set; } = string.Empty;

        public EmployeeRole Role { get; set; }

        // assistant or employee reports to someone
        // manager has null
        public int? ManagerId { get; set; }

        // soft delete flag
        public bool IsActive { get; set; } = true;
    }
}