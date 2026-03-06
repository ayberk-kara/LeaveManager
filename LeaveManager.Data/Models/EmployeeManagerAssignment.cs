namespace LeaveManager.Data.Models
{
    public sealed class EmployeeManagerAssignment
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public int ManagerId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}