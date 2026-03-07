using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;
using LeaveManager.Data.Storage;
using LeaveManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IzinProgrami
{
    /// <summary>
    /// Interaction logic for EmployeeManagerAssignmentsWindow.xaml
    /// </summary>
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
            lstAssignments.ItemsSource = _assignments.Select(a => new
            {
                a.Id,
                DisplayText = $"{a.Year}-{a.Month:D2} → Müdür ID: {a.ManagerId}"
            }).ToList();
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

        private void LstAssignments_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstAssignments.SelectedItem == null) return;

            dynamic selected = lstAssignments.SelectedItem;
            int assignmentId = selected.Id;

            try
            {
                // assignmentId ile gerçek nesneyi alıyoruz
                var assignment = _assignments.FirstOrDefault(a => a.Id == assignmentId);
                if (assignment == null)
                {
                    MessageBox.Show("Seçilen atama bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var editWindow = new EditAssignmentWindow(_employeeId, assignment);
                editWindow.Owner = this;
                editWindow.ShowDialog();
                LoadAssignments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Atama düzenleme açılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}