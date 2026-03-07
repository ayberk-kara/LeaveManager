using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;

namespace IzinProgrami
{
    public partial class EmployeeManagerAssignmentsWindow : Window
    {
        private readonly int _employeeId;
        private readonly EmployeeRepository _employeeRepo = new();
        private List<EmployeeManagerAssignmentDisplay> _assignments = new();

        public EmployeeManagerAssignmentsWindow(int employeeId)
        {
            InitializeComponent();
            _employeeId = employeeId;
            LoadAssignments();
        }

        private void LoadAssignments()
        {
            var dbAssignments = _employeeRepo.GetManagerAssignments(_employeeId);
            var allEmployees = _employeeRepo.GetAllActive();

            _assignments = dbAssignments.Select(a =>
            {
                var manager = allEmployees.FirstOrDefault(m => m.Id == a.ManagerId);
                string managerName = manager != null ? manager.FullName : "Bilinmiyor";

                DateTime start = new DateTime(a.Year, a.Month, 1);
                DateTime end = new DateTime(a.Year, a.Month, DateTime.DaysInMonth(a.Year, a.Month));

                return new EmployeeManagerAssignmentDisplay
                {
                    Id = a.Id,
                    ManagerId = a.ManagerId,
                    ManagerName = managerName,
                    Year = a.Year,
                    Month = a.Month,
                    Start = start,
                    End = end
                };
            }).OrderBy(a => a.Year).ThenBy(a => a.Month).ToList();

            lstAssignments.ItemsSource = _assignments;
        }

        private void BtnNewAssignment_Click(object sender, RoutedEventArgs e)
        {
            var newAssignmentWindow = new EditAssignmentWindow(_employeeId);
            newAssignmentWindow.Owner = this;
            if (newAssignmentWindow.ShowDialog() == true)
            {
                LoadAssignments();
            }
        }

        private void LstAssignments_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstAssignments.SelectedItem is EmployeeManagerAssignmentDisplay selected)
            {
                var editAssignmentWindow = new EditAssignmentWindow(_employeeId, selected);
                editAssignmentWindow.Owner = this;
                if (editAssignmentWindow.ShowDialog() == true)
                {
                    LoadAssignments();
                }
            }
        }
    }

    public class EmployeeManagerAssignmentDisplay
    {
        public int Id { get; set; }
        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string DisplayText => $"{ManagerName}: {Start:MM.yyyy}";
    }
}