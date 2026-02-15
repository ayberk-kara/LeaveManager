using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;

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
                MessageBox.Show(
                    "Önce soldan bir personel seçin.",
                    "Personel seçimi gerekli",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var dlg = new NewLeaveWindow(_vm.SelectedEmployee.Id)
            {
                Owner = this
            };

            var ok = dlg.ShowDialog();
            if (ok != true)
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

            var result = dialog.ShowDialog();

            if (result == true)
            {
                _vm.ReloadEmployeesFromDatabase();
            }
        }
    }

    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly LeaveRepository _leaveRepository = new();
        private readonly EmployeeRepository _employeeRepository = new();

        private string _searchText = string.Empty;
        private EmployeeItem? _selectedEmployee;
        private DateTime _baseMonth = DateTime.Today;
        private DateTime _selectedDay = DateTime.Today;

        private HashSet<DateTime> _selectedEmployeeLeaveDays = new();
        private List<Leave> _selectedEmployeeLeaves = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<EmployeeItem> AllEmployees { get; } = new();
        public ObservableCollection<EmployeeItem> Employees { get; } = new();
        public ObservableCollection<LeaveListItem> SelectedDayLeaves { get; } = new();

        public MainViewModel()
        {
            LoadEmployeesFromDatabase();
            ApplyEmployeeFilter();
            SetSelectedDay(DateTime.Today);
            UpdateHeaderHint();
        }

        private void LoadEmployeesFromDatabase()
        {
            AllEmployees.Clear();

            var employees = _employeeRepository.GetAllActive();

            foreach (var emp in employees)
            {
                AllEmployees.Add(new EmployeeItem(
                    emp.Id,
                    emp.FullName,
                    $"Sicil: {emp.SicilNo}"
                ));
            }
        }
        public void ReloadEmployeesFromDatabase()
        {
            LoadEmployeesFromDatabase();
            ApplyEmployeeFilter();
        }

        public DateTime BaseMonth
        {
            get => _baseMonth;
            set
            {
                if (_baseMonth == value) return;
                _baseMonth = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value ?? string.Empty;
                OnPropertyChanged();
                ApplyEmployeeFilter();
            }
        }

        public EmployeeItem? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                if (_selectedEmployee == value) return;
                _selectedEmployee = value;
                OnPropertyChanged();

                ReloadSelectedEmployeeLeavesFromDatabase();
                UpdateHeaderHint();
            }
        }

        public HashSet<DateTime> SelectedEmployeeLeaveDays
        {
            get => _selectedEmployeeLeaveDays;
            private set
            {
                _selectedEmployeeLeaveDays = value;
                OnPropertyChanged();
            }
        }

        public string HeaderHintText =>
            SelectedEmployee == null
                ? "Lütfen soldan bir personel seçin."
                : $"{SelectedEmployee.FullName} seçili.";

        public string SelectedDayTitle
        {
            get
            {
                var tr = new CultureInfo("tr-TR");
                return $"{_selectedDay.ToString("dd MMMM yyyy", tr)} — İzinler";
            }
        }

        public string SelectedDayEmptyHint =>
            SelectedEmployee == null
                ? "İzinleri görmek için önce personel seçin."
                : "Bu gün için kayıtlı izin yok.";

        public Visibility IsSelectedDayEmptyHintVisible =>
            SelectedDayLeaves.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

        public void SetSelectedDay(DateTime day)
        {
            _selectedDay = day.Date;
            OnPropertyChanged(nameof(SelectedDayTitle));
            RefreshSelectedDayLeaves();
        }

        public void ReloadSelectedEmployeeLeavesFromDatabase()
        {
            if (SelectedEmployee == null)
            {
                _selectedEmployeeLeaves = new List<Leave>();
                SelectedEmployeeLeaveDays = new HashSet<DateTime>();
                RefreshSelectedDayLeaves();
                return;
            }

            _selectedEmployeeLeaves = _leaveRepository.GetByEmployee(SelectedEmployee.Id);

            UpdateSelectedEmployeeLeaveDays();
            RefreshSelectedDayLeaves();

            OnPropertyChanged(nameof(IsSelectedDayEmptyHintVisible));
            OnPropertyChanged(nameof(SelectedDayEmptyHint));
        }



        private void RefreshSelectedDayLeaves()
        {
            SelectedDayLeaves.Clear();

            if (SelectedEmployee == null)
            {
                OnPropertyChanged(nameof(IsSelectedDayEmptyHintVisible));
                return;
            }

            var leaves = _selectedEmployeeLeaves
                .Where(l => IncludesDay(l.StartDate, l.EndDate, _selectedDay))
                .OrderBy(l => l.StartDate)
                .ToList();

            foreach (var leave in leaves)
            {
                SelectedDayLeaves.Add(new LeaveListItem
                {
                    Title = $"{leave.Type} İzni",
                    Subtitle = $"{leave.StartDate:dd.MM.yyyy} - {leave.EndDate:dd.MM.yyyy}"
                });
            }

            OnPropertyChanged(nameof(IsSelectedDayEmptyHintVisible));
        }

        private void UpdateSelectedEmployeeLeaveDays()
        {
            if (SelectedEmployee == null)
            {
                SelectedEmployeeLeaveDays = new HashSet<DateTime>();
                return;
            }

            var days = new HashSet<DateTime>();

            foreach (var leave in _selectedEmployeeLeaves)
            {
                var d = leave.StartDate.Date;
                var end = leave.EndDate.Date;

                while (d <= end)
                {
                    days.Add(d);
                    d = d.AddDays(1);
                }
            }

            SelectedEmployeeLeaveDays = days;
        }

        private static bool IncludesDay(DateTime start, DateTime end, DateTime day)
        {
            var d = day.Date;
            return d >= start.Date && d <= end.Date;
        }

        private void UpdateHeaderHint() => OnPropertyChanged(nameof(HeaderHintText));

        private void ApplyEmployeeFilter()
        {
            Employees.Clear();

            var query = (SearchText ?? string.Empty).Trim();
            IEnumerable<EmployeeItem> result = AllEmployees;

            if (!string.IsNullOrWhiteSpace(query))
            {
                result = result.Where(e =>
                    e.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    e.Subtitle.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var emp in result)
                Employees.Add(emp);
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }



    public sealed class EmployeeItem
    {
        public int Id { get; }
        public string FullName { get; }
        public string Subtitle { get; }

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

        public EmployeeItem(int id, string fullName, string subtitle)
        {
            Id = id;
            FullName = fullName;
            Subtitle = subtitle;
        }
    }

    public sealed class LeaveListItem
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
    }
}