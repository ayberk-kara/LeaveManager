using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace LeaveManager.App
{
    public partial class AddEmployeeChoiceWindow : Window
    {
        public AddEmployeeChoiceWindow()
        {
            InitializeComponent();
        }

        private void Single_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true; // Tekli
        }

        private void Bulk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Toplu
        }
    }
}
