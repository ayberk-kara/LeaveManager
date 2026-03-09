using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;
using LeaveManager.Data.Storage;
using LeaveManager.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IzinProgrami
{
    
    public class AssignmentDisplayItem
    {
        public int EmployeeId { get; set; }
        public int ManagerId { get; set; }
        public int Year { get; set; }
        public int MonthStart { get; set; }
        public int MonthEnd { get; set; }
        public string DisplayText { get; set; } = string.Empty;
    }

    public partial class EmployeeManagerAssignmentsWindow : Window
    {
        private readonly EmployeeRepository _repository = new();
        private readonly int _employeeId;
        private List<EmployeeManagerAssignment> _assignments = new();

        public EmployeeManagerAssignmentsWindow(int employeeId)
        {
            InitializeComponent();
            _employeeId = employeeId;

            LoadAssignments();
        }

        private void LoadAssignments()
        {
            _assignments = _repository.GetManagerAssignments(_employeeId);

            var grouped = _assignments
                .GroupBy(a => a.ManagerId)
                .SelectMany(g =>
                {
                    var manager = _repository.GetById(g.Key);
                    if (manager == null) return new List<AssignmentDisplayItem>();

                    var months = g.OrderBy(a => new DateTime(a.Year, a.Month, 1)).ToList();

                    List<AssignmentDisplayItem> ranges = new();
                    int startMonth = months[0].Month;
                    int startYear = months[0].Year;
                    int prevMonth = startMonth;
                    int prevYear = startYear;

                    for (int i = 1; i < months.Count; i++)
                    {
                        var current = months[i];
                        var nextMonth = prevMonth == 12 ? 1 : prevMonth + 1;
                        var nextYear = prevMonth == 12 ? prevYear + 1 : prevYear;

                        if (current.Month == nextMonth && current.Year == nextYear)
                        {
                            prevMonth = current.Month;
                            prevYear = current.Year;
                            continue;
                        }
                        else
                        {
                            ranges.Add(new AssignmentDisplayItem
                            {
                                EmployeeId = _employeeId,
                                ManagerId = manager.Id,
                                Year = prevYear,
                                MonthStart = startMonth,
                                MonthEnd = prevMonth,
                                DisplayText = $"{manager.FullName} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(startMonth)} {startYear} - {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(prevMonth)} {prevYear}"
                            });
                            startMonth = current.Month;
                            startYear = current.Year;
                            prevMonth = startMonth;
                            prevYear = startYear;
                        }
                    }

                    ranges.Add(new AssignmentDisplayItem
                    {
                        EmployeeId = _employeeId,
                        ManagerId = manager.Id,
                        Year = prevYear,
                        MonthStart = startMonth,
                        MonthEnd = prevMonth,
                        DisplayText = $"{manager.FullName} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(startMonth)} {startYear} - {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(prevMonth)} {prevYear}"
                    });

                    return ranges;
                })
                .OrderBy(a => a.DisplayText)
                .ToList();

            lstAssignments.ItemsSource = grouped;
        }

        private void BtnNewAssignment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editWindow = new EditAssignmentWindow(_employeeId);
                editWindow.Owner = this;
                editWindow.ShowDialog();
                LoadAssignments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yeni atama açılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditAssignment_Click(object sender, RoutedEventArgs e)
        {
            if (lstAssignments.SelectedItem is not AssignmentDisplayItem selected) return;

            var window = new EditAssignmentWindow(_employeeId,
                _assignments.First(a => a.ManagerId == selected.ManagerId && a.Year == selected.Year));
            window.Owner = this;

            window.AssignmentSaved += () =>
            {
                LoadAssignments();
            };

            window.ShowDialog();
        }

        private void DeleteAssignment_Click(object sender, RoutedEventArgs e)
        {
            if (lstAssignments.SelectedItem == null) return;

            var selected = lstAssignments.SelectedItem as AssignmentDisplayItem;
            if (selected == null) return;

            var confirmWindow = new LeaveManager.App.DeleteAssignmentWindow();
            confirmWindow.Owner = this;
            bool? result = confirmWindow.ShowDialog();

            if (result == true)
            {
                _repository.DeleteManagerAssignments(
                    selected.EmployeeId,
                    selected.ManagerId,
                    selected.Year);

                LoadAssignments();
            }
        }

        private void LstAssignments_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstAssignments.SelectedItem == null) return;

            var selected = lstAssignments.SelectedItem as AssignmentDisplayItem;
            if (selected == null) return;

            try
            {
                MessageBox.Show($"Atama düzenleme: {selected.DisplayText}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Atama düzenleme açılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}