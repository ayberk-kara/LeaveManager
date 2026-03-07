using System.Windows;

namespace LeaveManager.App
{
    public partial class DeleteAssignmentWindow : Window
    {
        public DeleteAssignmentWindow()
        {
            InitializeComponent();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            
            DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            
            DialogResult = false;
            this.Close();
        }
    }
}