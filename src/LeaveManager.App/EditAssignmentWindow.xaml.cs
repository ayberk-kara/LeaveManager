using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;
using LeaveManager.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IzinProgrami
{
    public partial class EditAssignmentWindow : Window
    {
        private readonly int _employeeId;
        private readonly EmployeeRepository _repository = new();

        private EmployeeManagerAssignment? _assignment;

        public event Action? AssignmentSaved;

        public EditAssignmentWindow(int employeeId)
        {
            InitializeComponent();

            _employeeId = employeeId;

            LoadManagers();
            LoadYearComboBox();
            LoadMonthsCheckList();
        }

        public EditAssignmentWindow(int employeeId, EmployeeManagerAssignment assignment)
            : this(employeeId)
        {
            _assignment = assignment;
            LoadAssignmentData();
        }

        private void LoadManagers()
        {
            var assistants = _repository.GetAssistants();

            cmbManager.ItemsSource = assistants;
            cmbManager.DisplayMemberPath = "FullName";
            cmbManager.SelectedValuePath = "Id";
        }

        private void LoadYearComboBox()
        {
            for (int year = 2015; year <= 2055; year++)
                cmbYear.Items.Add(year);

            cmbYear.SelectedItem = _assignment?.Year ?? DateTime.Now.Year;
        }

        private void LoadMonthsCheckList()
        {
            spMonths.Children.Clear();

            for (int month = 1; month <= 12; month++)
            {
                var cb = new CheckBox
                {
                    Content = CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetMonthName(month),
                    Tag = month,
                    Margin = new Thickness(0, 0, 12, 6)
                };

                spMonths.Children.Add(cb);
            }
        }

        private void LoadAssignmentData()
        {
            if (_assignment == null)
                return;

            cmbManager.SelectedValue = _assignment.ManagerId;
            cmbYear.SelectedItem = _assignment.Year;

            foreach (CheckBox cb in spMonths.Children)
            {
                if (cb.Tag is int month && month == _assignment.Month)
                    cb.IsChecked = true;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbManager.SelectedItem is not Employee manager)
            {
                MessageBox.Show("Müdür Yardımcısı seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbYear.SelectedItem is not int year)
            {
                MessageBox.Show("Geçerli bir yıl seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedMonths = spMonths.Children
                .OfType<CheckBox>()
                .Where(x => x.IsChecked == true)
                .Select(x => Convert.ToInt32(x.Tag))
                .ToList();

            if (!selectedMonths.Any())
            {
                MessageBox.Show("En az bir ay seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
               

                if (_assignment != null)
                {
                    _repository.DeleteManagerAssignments(
                        _employeeId,
                        _assignment.ManagerId,
                        _assignment.Year);
                }

                
               

                _repository.SaveManagerAssignments(
                    _employeeId,
                    manager.Id,
                    year,
                    selectedMonths);

                AssignmentSaved?.Invoke();

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}