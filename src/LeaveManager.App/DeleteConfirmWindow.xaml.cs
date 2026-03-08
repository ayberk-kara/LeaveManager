using LeaveManager.Data.Repositories;
using System;
using System.Windows;

namespace LeaveManager.App
{
    public partial class DeleteConfirmWindow : Window
    {
        private readonly int _employeeId;
        private readonly EmployeeRepository _repository = new();

        public bool Confirmed { get; private set; }

        public DeleteConfirmWindow(int employeeId)
        {
            InitializeComponent();
            _employeeId = employeeId;

            
            txtMessage.Text = "Bu personeli silmek, onun tüm izinlerini, manager atamalarını ve tüm ilişkili verilerini kalıcı olarak silecektir. Devam etmek istediğinize emin misiniz?";
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // HARD DELETE çağrısı
                _repository.HardDelete(_employeeId);

                Confirmed = true;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Personel silinemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}