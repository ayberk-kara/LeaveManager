using LeaveManager.Data.Repositories;
using LeaveManager.Data.Storage;
using LeaveManager.Data.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LeaveManager.App
{
    public partial class ManageLeavesWindow : Window
    {
        private readonly LeaveRepository _leaveRepository = new();
        private readonly int _employeeId;
        private List<SelectableLeave> _leaves = new();

        public ManageLeavesWindow(int employeeId, string employeeName)
        {
            InitializeComponent();
            _employeeId = employeeId;
            txtEmployeeName.Text = employeeName;
            LoadLeaves();
        }

        private void LoadLeaves()
        {
            var connectionString = $"Data Source={DbPaths.GetDbFilePath()}";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var leaves = _leaveRepository.GetByEmployeeId(connection, _employeeId);

            _leaves = leaves.Select(l => new SelectableLeave { Leave = l, IsSelected = false }).ToList();

            lvLeaves.ItemsSource = _leaves;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var selectedLeaves = _leaves.Where(l => l.IsSelected).Select(l => l.Leave).ToList();

            if (!selectedLeaves.Any())
            {
                MessageBox.Show("Silmek için izin seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Seçili izinler silinsin mi?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var connectionString = $"Data Source={DbPaths.GetDbFilePath()}";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();

            foreach (var leave in selectedLeaves)
                _leaveRepository.Delete(connection, tx, leave.Id);

            tx.Commit();

            LoadLeaves();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Wrapper sınıf: ListView'de checkbox ile bağlanacak
        private class SelectableLeave
        {
            public Leave Leave { get; set; } = null!;
            public bool IsSelected { get; set; }
        }
    }
}