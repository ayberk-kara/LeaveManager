namespace LeaveManager.Data.Models
{
    public sealed class EmployeeManagerAssignment
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public int ManagerId { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }
    }
}