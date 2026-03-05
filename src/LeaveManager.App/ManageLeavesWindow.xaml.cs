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
        private List<Leave> _leaves = new();

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

            _leaves = _leaveRepository.GetByEmployeeId(connection, _employeeId).ToList();

            var items = _leaves.Select(l => new
            {
                Leave = l,
                DisplayText = $"{l.Type} | {l.StartDate:dd/MM/yyyy} - {l.EndDate:dd/MM/yyyy} ({(l.EndDate - l.StartDate).Days + 1} gün)"
            }).ToList();

            lstLeaves.ItemsSource = items;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (lstLeaves.SelectedItems.Count == 0)
            {
                MessageBox.Show("Silmek için izin seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Seçili izinler silinsin mi?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var selectedLeaves = lstLeaves.SelectedItems.Cast<dynamic>().Select(x => x.Leave).ToList();

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
    }
}