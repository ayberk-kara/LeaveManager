using System;
using System.Collections.Generic;
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

        private readonly Dictionary<EmployeeRole, string> _roleMap = new()
        {
            { EmployeeRole.Assistant, "Müdür Yardımcısı" },
            { EmployeeRole.Employee, "Personel" }
        };

        public EditEmployeeWindow(string fullName, int sicilNo, EmployeeRole role)
        {
            InitializeComponent();

            // Initial values
            txtName.Text = fullName;
            txtRegistryNo.Text = sicilNo.ToString();

            // Rol ComboBox
            cmbRole.ItemsSource = new List<string>(_roleMap.Values);
            cmbRole.SelectedItem = _roleMap[role];
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
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

            UpdatedName = txtName.Text.Trim();
            UpdatedSicilNo = sicilNo;

            // Rolu enum olarak geri çevir
            foreach (var kv in _roleMap)
            {
                if (kv.Value == cmbRole.SelectedItem.ToString())
                {
                    UpdatedRole = kv.Key;
                    break;
                }
            }

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