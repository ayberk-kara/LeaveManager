using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;
using LeaveManager.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace LeaveManager.App
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            _vm = new MainViewModel();
            DataContext = _vm;

            SetCalendarsToBaseMonth(_vm.BaseMonth);
        }

        private void ManageEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedEmployee == null)
                return;

            var dialog = new EditEmployeeWindow(
                _vm.SelectedEmployee.FullName,
                _vm.SelectedEmployee.SicilNo,
                _vm.SelectedEmployee.Role,
                _vm.SelectedEmployee.ManagerId) 
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                if (dialog.IsDeleteRequested)
                {
                    _vm.DeleteEmployee(_vm.SelectedEmployee.Id);
                }
                else
                {
                    _vm.UpdateEmployee(
                        _vm.SelectedEmployee.Id,
                        dialog.UpdatedName,
                        dialog.UpdatedSicilNo,
                        dialog.UpdatedRole,
                        dialog.UpdatedManagerId); 
                }
            }
        }

        // -------- Calendar logic --------

        private static DateTime NormalizeToMonthStart(DateTime anyDate)
            => new DateTime(anyDate.Year, anyDate.Month, 1);

        private static DateTime MonthEnd(DateTime monthStart)
            => monthStart.AddMonths(1).AddDays(-1);

        private void SetOneCalendarToMonth(System.Windows.Controls.Calendar cal, DateTime monthStart)
        {
            var start = NormalizeToMonthStart(monthStart);
            var end = MonthEnd(start);

            cal.DisplayDateStart = start;
            cal.DisplayDateEnd = end;
            cal.DisplayDate = start;
        }

        private void SetCalendarsToBaseMonth(DateTime baseMonth)
        {
            var baseStart = NormalizeToMonthStart(baseMonth);

            SetOneCalendarToMonth(Cal1, baseStart);
            SetOneCalendarToMonth(Cal2, baseStart.AddMonths(1));
            SetOneCalendarToMonth(Cal3, baseStart.AddMonths(2));
            SetOneCalendarToMonth(Cal4, baseStart.AddMonths(3));
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _vm.BaseMonth = NormalizeToMonthStart(_vm.BaseMonth).AddMonths(-1);
            SetCalendarsToBaseMonth(_vm.BaseMonth);
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _vm.BaseMonth = NormalizeToMonthStart(_vm.BaseMonth).AddMonths(1);
            SetCalendarsToBaseMonth(_vm.BaseMonth);
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            _vm.BaseMonth = NormalizeToMonthStart(DateTime.Today);
            SetCalendarsToBaseMonth(_vm.BaseMonth);
        }

        private void NewLeave_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedEmployee == null)
            {
                MessageBox.Show("Önce soldan bir personel seçin.");
                return;
            }

            var dlg = new NewLeaveWindow(_vm.SelectedEmployee.Id)
            {
                Owner = this
            };

            if (dlg.ShowDialog() != true)
                return;

            _vm.ReloadSelectedEmployeeLeavesFromDatabase();
            _vm.BaseMonth = NormalizeToMonthStart(dlg.StartDate);
            SetCalendarsToBaseMonth(_vm.BaseMonth);
        }

        private void Calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Calendar calendar)
                return;

            if (calendar.SelectedDate == null)
                return;

            var selected = calendar.SelectedDate.Value.Date;
            _vm.SetSelectedDay(selected);

            if (!ReferenceEquals(calendar, Cal1)) Cal1.SelectedDate = null;
            if (!ReferenceEquals(calendar, Cal2)) Cal2.SelectedDate = null;
            if (!ReferenceEquals(calendar, Cal3)) Cal3.SelectedDate = null;
            if (!ReferenceEquals(calendar, Cal4)) Cal4.SelectedDate = null;
        }

        private void NewEmployee_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEmployeeWindow
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
                _vm.ReloadEmployeesFromDatabase();
        }
    }

    // ================= VIEW MODEL =================

    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly LeaveRepository _leaveRepository = new();
        private readonly EmployeeRepository _employeeRepository = new();

        private EmployeeItem? _selectedEmployee;
        private DateTime _baseMonth = DateTime.Today;
        private DateTime? _selectedDay;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<EmployeeItem> Employees { get; } = new();
        public ObservableCollection<LeaveItem> SelectedEmployeeLeaves { get; } = new();

        public MainViewModel()
        {
            ReloadEmployeesFromDatabase();
        }

        public DateTime BaseMonth
        {
            get => _baseMonth;
            set
            {
                _baseMonth = value;
                OnPropertyChanged();
            }
        }

        public EmployeeItem? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged();
            }
        }

        public DateTime? SelectedDay
        {
            get => _selectedDay;
            private set
            {
                _selectedDay = value;
                OnPropertyChanged();
            }
        }

        public void SetSelectedDay(DateTime day)
        {
            SelectedDay = day;
            
        }

        public void ReloadSelectedEmployeeLeavesFromDatabase()
        {
            SelectedEmployeeLeaves.Clear();

            if (SelectedEmployee == null)
                return;

            var leaves = _leaveRepository.GetByEmployeeId(SelectedEmployee.Id);

            foreach (var leave in leaves)
            {
                SelectedEmployeeLeaves.Add(new LeaveItem(
                    leave.Id,
                    leave.StartDate,
                    leave.EndDate,
                    leave.Type));
            }
        }

        public void ReloadEmployeesFromDatabase()
        {
            Employees.Clear();

            var employees = _employeeRepository.GetAllActive();

            foreach (var emp in employees)
            {
                Employees.Add(new EmployeeItem(
                    emp.Id,
                    emp.FullName,
                    emp.SicilNo,
                    emp.Role,
                    emp.ManagerId));
            }
        }

        public void UpdateEmployee(int id, string fullName, int sicilNo, EmployeeRole role, int? managerId)
        {
            var employee = _employeeRepository.GetById(id);
            if (employee == null) return;

            employee.FullName = fullName;
            employee.SicilNo = sicilNo;
            employee.Role = role;
            employee.ManagerId = managerId;

            _employeeRepository.Update(employee);

            ReloadEmployeesFromDatabase();
        }

        public void DeleteEmployee(int id)
        {
            _employeeRepository.SoftDelete(id);
            ReloadEmployeesFromDatabase();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ================= EMPLOYEE ITEM =================

    public sealed class EmployeeItem
    {
        public int Id { get; }
        public string FullName { get; }
        public int SicilNo { get; }
        public EmployeeRole Role { get; }
        public int? ManagerId { get; } // <-- ekledik

        public string Subtitle => $"Sicil: {SicilNo}";

        public string Initials
        {
            get
            {
                var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return "?";
                if (parts.Length == 1) return parts[0][0].ToString().ToUpperInvariant();
                return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpperInvariant();
            }
        }

        public EmployeeItem(int id, string fullName, int sicilNo, EmployeeRole role, int? managerId)
        {
            Id = id;
            FullName = fullName;
            SicilNo = sicilNo;
            Role = role;
            ManagerId = managerId; // <-- set
        }
    }

    // ================= LEAVE ITEM =================

    public sealed class LeaveItem
    {
        public int Id { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public string Type { get; }

        public LeaveItem(int id, DateTime startDate, DateTime endDate, string type)
        {
            Id = id;
            StartDate = startDate;
            EndDate = endDate;
            Type = type;
        }
    }
}