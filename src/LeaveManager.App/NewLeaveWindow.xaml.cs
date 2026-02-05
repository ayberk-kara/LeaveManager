using System;
using System.Windows;
using System.Windows.Controls;
using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;

namespace LeaveManager.App
{
    public partial class NewLeaveWindow : Window
    {
        // We keep these for UI flow (MainWindow can still read them if needed)
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public string LeaveType { get; private set; } = "Yıllık";

        // Sprint 1: we must know for which employee we are saving the leave.
        public int EmployeeId { get; }

        public bool IsSavedToDatabase { get; private set; }

        public NewLeaveWindow(int employeeId)
        {
            InitializeComponent();

            EmployeeId = employeeId;

            // Default dates: today
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;

            // Default type: first item
            LeaveTypeCombo.SelectedIndex = 0;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsSavedToDatabase = false;
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // validate dates
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show(
                    "Başlangıç ve bitiş tarihlerini seçin.",
                    "Eksik bilgi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var start = StartDatePicker.SelectedDate.Value.Date;
            var end = EndDatePicker.SelectedDate.Value.Date;

            if (end < start)
            {
                MessageBox.Show(
                    "Bitiş tarihi başlangıç tarihinden önce olamaz.",
                    "Geçersiz tarih aralığı",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // resolve type from ComboBoxItem 
            if (LeaveTypeCombo.SelectedItem is ComboBoxItem item && item.Content != null)
                LeaveType = item.Content.ToString() ?? "Yıllık";
            else
                LeaveType = "Yıllık";

            StartDate = start;
            EndDate = end;

            try
            {
                var repo = new LeaveRepository();

                repo.Add(new Leave
                {
                    EmployeeId = EmployeeId,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    Type = LeaveType,
                    CreatedAt = DateTime.Now
                });

                IsSavedToDatabase = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Kayıt sırasında bir hata oluştu.\n\nDetay: " + ex.Message,
                    "Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
