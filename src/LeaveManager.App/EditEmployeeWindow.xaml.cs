using System.Windows;

namespace LeaveManager.App
{
    public partial class EditEmployeeWindow : Window
    {
        public EditEmployeeWindow(string fullName, int sicilNo)
        {
            InitializeComponent();

            txtName.Text = fullName;
            txtRegistryNo.Text = sicilNo.ToString();
        }

        public string UpdatedName => txtName.Text.Trim();

        public int UpdatedSicilNo =>
            int.TryParse(txtRegistryNo.Text, out var no) ? no : 0;

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtRegistryNo.Text))
            {
                MessageBox.Show("Tüm alanlar doldurulmalıdır.");
                return;
            }

            if (!int.TryParse(txtRegistryNo.Text, out _))
            {
                MessageBox.Show("Sicil No sadece sayı olmalıdır.");
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}