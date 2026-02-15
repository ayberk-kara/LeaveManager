using System;
using System.Windows;
using LeaveManager.Models;

namespace LeaveManager.App
{
    public partial class EditEmployeeWindow : Window
    {
        // --- OUTPUT PROPERTIES ---
        public string UpdatedName { get; private set; }
        public int UpdatedSicilNo { get; private set; }
        public EmployeeRole UpdatedRole { get; private set; }
        public bool IsDeleteRequested { get; private set; }

        public EditEmployeeWindow(string fullName, int sicilNo, EmployeeRole role)
        {
            InitializeComponent();

            // Initial values
            txtName.Text = fullName;
            txtRegistryNo.Text = sicilNo.ToString();

            cmbRole.ItemsSource = Enum.GetValues(typeof(EmployeeRole));
            cmbRole.SelectedItem = role;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // --- VALIDATION ---

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Ad Soyad boş olamaz.", "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtRegistryNo.Text, out int sicilNo))
            {
                MessageBox.Show("Geçerli bir sicil numarası giriniz.", "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbRole.SelectedItem == null)
            {
                MessageBox.Show("Rol seçiniz.", "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // --- SET OUTPUT VALUES ---
            UpdatedName = txtName.Text.Trim();
            UpdatedSicilNo = sicilNo;
            UpdatedRole = (EmployeeRole)cmbRole.SelectedItem;

            IsDeleteRequested = false;

            DialogResult = true;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var confirm = new DeleteConfirmWindow
            {
                Owner = this
            };

            confirm.ShowDialog();

            if (confirm.IsConfirmed)
            {
                IsDeleteRequested = true;
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}