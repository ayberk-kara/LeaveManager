using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;
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

namespace IzinProgrami
{
    /// <summary>
    /// Interaction logic for EmployeeManagerAssignmentsWindow.xaml
    /// </summary>
    public partial class EmployeeManagerAssignmentsWindow : Window
    {
        private readonly int _employeeId;
        private readonly EmployeeRepository _repository = new();

        public EmployeeManagerAssignmentsWindow(int employeeId)
        {
            InitializeComponent();
            _employeeId = employeeId;
            LoadAssignments();
        }

        private void LoadAssignments()
        {
            var assignments = _repository.GetManagerAssignments(_employeeId);
            lstAssignments.ItemsSource = assignments;
        }

        private void BtnNewAssignment_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditAssignmentWindow(_employeeId);
            editWindow.Owner = this;
            if (editWindow.ShowDialog() == true)
            {
                LoadAssignments();
            }
        }

        private void LstAssignments_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstAssignments.SelectedItem is EmployeeManagerAssignment assignment)
            {
                var editWindow = new EditAssignmentWindow(_employeeId, assignment);
                editWindow.Owner = this;
                if (editWindow.ShowDialog() == true)
                {
                    LoadAssignments();
                }
            }
        }
    }
}
