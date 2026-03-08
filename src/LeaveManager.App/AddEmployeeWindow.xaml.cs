using System;
using System.Windows;
using System.Windows.Controls;
using LeaveManager.Data.Repositories;
using LeaveManager.Models;

namespace LeaveManager.App
{
    public partial class AddEmployeeWindow : Window
    {
        private readonly EmployeeRepository _repository = new();

        public AddEmployeeWindow()
        {
            InitializeComponent();
        }

        private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoleComboBox.SelectedIndex == 1) // Personel
            {
                ManagerAssistantPanel.Visibility = Visibility.Visible;
                ManagerAssistantComboBox.Visibility = Visibility.Collapsed;
                ManagerAssistantInfoText.Visibility = Visibility.Visible;
            }
            else
            {
                ManagerAssistantPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(SicilTextBox.Text.Trim(), out int sicilNo))
                    throw new Exception("Sicil numarası geçersiz.");

                var fullName = NameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                    throw new Exception("Ad Soyad boş olamaz.");

                if (RoleComboBox.SelectedIndex == -1)
                    throw new Exception("Rol seçmelisiniz.");

                var role = RoleComboBox.SelectedIndex == 0
                    ? EmployeeRole.Assistant
                    : EmployeeRole.Employee;

                
                int? managerId = null;

                var restoredOrNew = _repository.RestoreOrCreate(
                    sicilNo,
                    fullName,
                    role,
                    managerId
                );

                if (restoredOrNew.WasRestored)
                {
                    MessageBox.Show(
                        "Bu çalışan daha önce silinmişti. Kaydı geri getirildi.",
                        "Bilgi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}