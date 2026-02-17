using System;
using System.Windows;
using System.Windows.Controls;
using LeaveManager.Data.Models;
using LeaveManager.App.Services;   // 🔹 Artık Business değil

namespace LeaveManager.App
{
    public partial class NewLeaveWindow : Window
    {
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public string LeaveType { get; private set; } = "Yıllık";

        public int EmployeeId { get; }

        public bool IsSavedToDatabase { get; private set; }

        private readonly LeaveService _leaveService;

        public NewLeaveWindow(int employeeId)
        {
            InitializeComponent();

            EmployeeId = employeeId;
            _leaveService = new LeaveService();

            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;
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

            if (LeaveTypeCombo.SelectedItem is ComboBoxItem item && item.Content != null)
                LeaveType = item.Content.ToString() ?? "Yıllık";
            else
                LeaveType = "Yıllık";

            StartDate = start;
            EndDate = end;

            try
            {
                var leave = new Leave
                {
                    EmployeeId = EmployeeId,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    Type = LeaveType,
                    CreatedAt = DateTime.Now
                };

                if (!_leaveService.TryAddLeave(leave, out string error))
                {
                    MessageBox.Show(
                        error,
                        "Kural ihlali",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

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