using System;
using System.Windows;
using System.Windows.Controls;

namespace LeaveManager.App
{
    public partial class NewLeaveWindow : Window
    {
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public string LeaveType { get; private set; } = "Yıllık";

        public NewLeaveWindow()
        {
            InitializeComponent();

            // Default dates: today
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;
            LeaveTypeCombo.SelectedIndex = 0;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
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

            // Type
            if (LeaveTypeCombo.SelectedItem is ComboBoxItem item && item.Content != null)
                LeaveType = item.Content.ToString() ?? "Yıllık";
            else
                LeaveType = "Yıllık";

            StartDate = start;
            EndDate = end;

            DialogResult = true;
            Close();
        }
    }
}
