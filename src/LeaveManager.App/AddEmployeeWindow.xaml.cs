using System.Windows;
using System.Windows.Controls;

namespace LeaveManager.App
{
    public partial class AddEmployeeWindow : Window
    {
        public AddEmployeeWindow()
        {
            InitializeComponent();
        }

        private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoleComboBox.SelectedItem is ComboBoxItem item)
            {
                if (item.Content.ToString() == "Personel")
                {
                    ManagerAssistantPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    ManagerAssistantPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("only ui for now");
        }
    }
}