using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using LeaveManager.Models;
using LeaveManager.Data.Repositories;

namespace LeaveManager.App
{
    public partial class EditEmployeeWindow : Window
    {
        // --- OUTPUT ---
        public string UpdatedName { get; private set; }
        public int UpdatedSicilNo { get; private set; }
        public EmployeeRole UpdatedRole { get; private set; }
        public int? UpdatedManagerId { get; private set; }
        public bool IsDeleteRequested { get; private set; }

        private readonly EmployeeRepository _repository = new();
        private readonly Dictionary<EmployeeRole, string> _roleMap = new()
        {
            { EmployeeRole.Assistant, "Müdür Yardımcısı" },
            { EmployeeRole.Employee, "Personel" }
        };

        private List<Employee> _assistants = new();

        public EditEmployeeWindow(string fullName, int sicilNo, EmployeeRole role, int? managerId)
        {
            InitializeComponent();

            txtName.Text = fullName;
            txtRegistryNo.Text = sicilNo.ToString();

            cmbRole.ItemsSource = new List<string>(_roleMap.Values);
            cmbRole.SelectedItem = _roleMap[role];

           
            _assistants = _repository.GetAssistants();
            cmbManagerAssistant.ItemsSource = _assistants;
            cmbManagerAssistant.DisplayMemberPath = "FullName";
            cmbManagerAssistant.SelectedValuePath = "Id";

            if (role == EmployeeRole.Employee)
            {
                ManagerAssistantPanel.Visibility = Visibility.Visible;
                if (managerId.HasValue)
                    cmbManagerAssistant.SelectedValue = managerId.Value; 
            }
            else
            {
                ManagerAssistantPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRole.SelectedItem?.ToString() == _roleMap[EmployeeRole.Employee])
                ManagerAssistantPanel.Visibility = Visibility.Visible;
            else
                ManagerAssistantPanel.Visibility = Visibility.Collapsed;
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

            foreach (var kv in _roleMap)
            {
                if (kv.Value == cmbRole.SelectedItem.ToString())
                {
                    UpdatedRole = kv.Key;
                    break;
                }
            }

            if (UpdatedRole == EmployeeRole.Employee && cmbManagerAssistant.SelectedItem is Employee selectedAssistant)
            {
                UpdatedManagerId = selectedAssistant.Id;
            }
            else
            {
                UpdatedManagerId = null;
            }

            IsDeleteRequested = false;
            DialogResult = true;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var confirm = new DeleteConfirmWindow { Owner = this };
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