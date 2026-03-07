using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;
using LeaveManager.Models;
using System;
using System.Collections.Generic;
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
            cmbYear.SelectedItem = DateTime.Now.Year;
        }

        private void LoadMonthsCheckList()
        {
            
            for (int month = 1; month <= 12; month++)
            {
                var cb = new CheckBox
                {
                    Content = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                    Tag = month,
                    Margin = new Thickness(0, 0, 12, 6)
                };
                spMonths.Children.Add(cb);
            }
        }

        private void LoadAssignmentData()
        {
            if (_assignment == null) return;

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
                .Where(cb => cb.IsChecked == true)
                .Select(cb =>
                {
                    if (cb.Tag is int i) return i;

                    
                    if (cb.Tag is string s && int.TryParse(s, out int n)) return n;

                   
                    return 0;
                })
                .Where(m => m > 0) 
                .ToList();

            if (!selectedMonths.Any())
            {
                MessageBox.Show("En az bir ay seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                
                foreach (var month in selectedMonths)
                {
                    _repository.SaveManagerAssignments(
                        _employeeId,
                        manager.Id,
                        year,
                        new List<int> { month }
                    );
                }

                
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
            this.Close();
        }
    }
}