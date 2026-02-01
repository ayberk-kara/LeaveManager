using System;
using System.Windows;

namespace LeaveManager.App
{
    public partial class NewLeaveWindow : Window
    {
        public NewLeaveWindow()
        {
            InitializeComponent();

            // Default dates: today
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // For now: only close. Next step: validate + return data.
            DialogResult = true;
            Close();
        }
    }
}
