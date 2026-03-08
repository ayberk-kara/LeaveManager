using LeaveManager.Data.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LeaveManager.App
{
    public partial class YearSelectionWindow : Window
    {
        public int? SelectedYear { get; private set; }

        public YearSelectionWindow()
        {
            InitializeComponent();
            LoadYears();
        }

        private void LoadYears()
        {
            var employeeRepository = new EmployeeRepository();
            List<int> years = employeeRepository.GetYearsWithLeaves();
            years.Sort();
            cmbYear.ItemsSource = years;
            if (years.Any())
                cmbYear.SelectedIndex = 0;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (cmbYear.SelectedItem != null)
            {
                SelectedYear = (int)cmbYear.SelectedItem;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Lütfen bir yıl seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}