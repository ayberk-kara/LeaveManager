using IzinProgrami;
using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;
using LeaveManager.Data.Storage;
using LeaveManager.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LeaveManager.App
{
    public partial class EditEmployeeWindow : Window
    {
        public string UpdatedName { get; private set; } = string.Empty;
        public int UpdatedSicilNo { get; private set; }
        public EmployeeRole UpdatedRole { get; private set; }
        public int? UpdatedManagerId { get; private set; }
        public bool IsDeleteRequested { get; private set; }

        public event Action<int>? EmployeeUpdated;

        private readonly EmployeeRepository _repository = new();
        private readonly LeaveBalanceRepository _balanceRepository = new();

        private LeaveBalance? _currentBalance;
        private readonly int _employeeId;
        private readonly EmployeeRole _employeeRole;

        private readonly Dictionary<EmployeeRole, string> _roleMap = new()
        {
            { EmployeeRole.Assistant, "Müdür Yardımcısı" },
            { EmployeeRole.Employee, "Personel" }
        };

        private List<Employee> _assistants = new();

        public EditEmployeeWindow(int employeeId,
                                  string fullName,
                                  int sicilNo,
                                  EmployeeRole role,
                                  int? managerId)
        {
            InitializeComponent();

            _employeeId = employeeId;
            _employeeRole = role;

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

                btnManageAssignments.Visibility = Visibility.Visible;
            }
            else
            {
                ManagerAssistantPanel.Visibility = Visibility.Collapsed;
                btnManageAssignments.Visibility = Visibility.Collapsed;
            }

            LoadCurrentBalance();
        }

        private void LoadCurrentBalance()
        {
            int currentYear = DateTime.Now.Year;

            using var connection = new SqliteConnection($"Data Source={DbPaths.GetDbFilePath()}");
            connection.Open();

            _currentBalance = _balanceRepository.GetByEmployeeAndYear(connection, _employeeId, currentYear);

            if (_currentBalance == null)
                return;

            int remainingAnnual =
                _currentBalance.AnnualEntitled +
                _currentBalance.AnnualManualAdjust -
                _currentBalance.AnnualUsed;

            int remainingSick =
                _currentBalance.SickEntitled +
                _currentBalance.SickManualAdjust -
                _currentBalance.SickUsed;

            txtRemainingAnnual.Text = remainingAnnual.ToString();
            txtRemainingSick.Text = remainingSick.ToString();
        }

        private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRole.SelectedItem?.ToString() == _roleMap[EmployeeRole.Employee])
            {
                ManagerAssistantPanel.Visibility = Visibility.Visible;
                btnManageAssignments.Visibility = Visibility.Visible;
            }
            else
            {
                ManagerAssistantPanel.Visibility = Visibility.Collapsed;
                btnManageAssignments.Visibility = Visibility.Collapsed;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtRemainingAnnual.Text, out int newAnnual) ||
                !int.TryParse(txtRemainingSick.Text, out int newSick))
            {
                MessageBox.Show("Geçerli izin değeri giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newAnnual < 0 || newSick < 0)
            {
                MessageBox.Show("Negatif izin girilemez.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_currentBalance != null)
            {
                using var connection = new SqliteConnection($"Data Source={DbPaths.GetDbFilePath()}");
                connection.Open();
                using var tx = connection.BeginTransaction();

                int baseAnnual = _currentBalance.AnnualEntitled - _currentBalance.AnnualUsed;
                int baseSick = _currentBalance.SickEntitled - _currentBalance.SickUsed;

                _currentBalance.AnnualManualAdjust = newAnnual - baseAnnual;
                _currentBalance.SickManualAdjust = newSick - baseSick;

                _balanceRepository.Update(connection, tx, _currentBalance);
                tx.Commit();
            }

            UpdatedName = txtName.Text.Trim();
            UpdatedSicilNo = int.Parse(txtRegistryNo.Text);

            foreach (var kv in _roleMap)
                if (kv.Value == cmbRole.SelectedItem?.ToString())
                    UpdatedRole = kv.Key;

            if (UpdatedRole == EmployeeRole.Employee &&
                cmbManagerAssistant.SelectedItem is Employee selected)
                UpdatedManagerId = selected.Id;
            else
                UpdatedManagerId = null;

            IsDeleteRequested = false;
            EmployeeUpdated?.Invoke(_employeeId);
            DialogResult = true;
        }

        private void ManageLeaves_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var manageLeavesWindow = new ManageLeavesWindow(_employeeId, txtName.Text.Trim());
                manageLeavesWindow.Owner = this;
                manageLeavesWindow.ShowDialog();

                LoadCurrentBalance();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İzin yönetim penceresi açılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManageAssignments_Click(object sender, RoutedEventArgs e)
        {
            if (_employeeRole != EmployeeRole.Employee) return;

            var assignmentsWindow = new EmployeeManagerAssignmentsWindow(_employeeId);
            assignmentsWindow.Owner = this;
            assignmentsWindow.ShowDialog();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            IsDeleteRequested = true;
            EmployeeUpdated?.Invoke(_employeeId);
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}