using ClosedXML.Excel;
using LeaveManager.App;
using LeaveManager.Data.Repositories;
using LeaveManager.Data.Storage;
using LeaveManager.App.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LeaveManager.App.Services
{
    public sealed class ExcelExportService : IExportService
    {
        private readonly LeaveRepository _leaveRepository = new();

        public void ExportAnnualPlanToExcel(IEnumerable<EmployeeItem> employees, int year)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Izin_Plani_{year}.xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("İzin Planı");

            WriteHeader(sheet, year);

            int row = 2;
            int index = 1;

            var connectionString = $"Data Source={DbPaths.GetDbFilePath()}";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            
            var managerColors = GenerateManagerColors(employees);

            var monthlyTotals = new int[12];
            int grandTotal = 0;
            int grandPlanned = 0;
            int grandRemaining = 0;

            foreach (var employee in employees)
            {
                sheet.Cell(row, 1).Value = index++;
                sheet.Cell(row, 2).Value = employee.FullName;

                
                var managerKey = employee.ManagerId ?? 0;
                ApplyRowColor(sheet, row, managerColors[managerKey]);

                var leaves = _leaveRepository.GetByEmployeeId(connection, employee.Id);

                var annualLeaves = leaves
                    .Where(l => l.Type.ToLower().Contains("yıllık")
                                && (l.StartDate.Year <= year && l.EndDate.Year >= year))
                    .ToList();

                var monthly = BuildMonthlySummary(annualLeaves, year);
                int yearlyTotal = CalculateYearlyTotalDays(annualLeaves, year);

                sheet.Cell(row, 3).Value = yearlyTotal;
                grandTotal += yearlyTotal;

                for (int month = 1; month <= 12; month++)
                {
                    if (monthly.ContainsKey(month))
                    {
                        sheet.Cell(row, month + 3).Value = monthly[month];
                        monthlyTotals[month - 1] += ExtractDaysFromText(monthly[month]);
                    }

                    sheet.Cell(row, month + 3).Style.Alignment.WrapText = true;
                }

                sheet.Cell(row, 16).Value = yearlyTotal;
                grandPlanned += yearlyTotal;

                int remaining = GetRemainingAnnualLeave(connection, employee.Id, year);
                sheet.Cell(row, 17).Value = remaining;
                grandRemaining += remaining;

                row++;
            }

            
            sheet.Cell(row, 2).Value = "TOPLAM";
            sheet.Cell(row, 2).Style.Font.Bold = true;

            sheet.Cell(row, 3).Value = grandTotal;

            for (int i = 0; i < 12; i++)
                sheet.Cell(row, i + 4).Value = monthlyTotals[i];

            sheet.Cell(row, 16).Value = grandPlanned;
            sheet.Cell(row, 17).Value = grandRemaining;

            sheet.Range(row, 1, row, 17).Style.Font.Bold = true;
            sheet.Range(row, 1, row, 17).Style.Fill.BackgroundColor = XLColor.LightGray;

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);
        }

        
        private static Dictionary<int, XLColor> GenerateManagerColors(IEnumerable<EmployeeItem> employees)
        {
            var managers = employees
                .Select(e => e.ManagerId ?? 0)
                .Distinct()
                .ToList();

            var palette = new[]
            {
                XLColor.FromHtml("#D9EAD3"),
                XLColor.FromHtml("#CFE2F3"),
                XLColor.FromHtml("#FCE5CD"),
                XLColor.FromHtml("#EAD1DC"),
                XLColor.FromHtml("#FFF2CC")
            };

            var result = new Dictionary<int, XLColor>();

            for (int i = 0; i < managers.Count; i++)
                result[managers[i]] = palette[i % palette.Length];

            return result;
        }

        private static void ApplyRowColor(IXLWorksheet sheet, int row, XLColor color)
        {
            sheet.Range(row, 1, row, 17).Style.Fill.BackgroundColor = color;
        }

        private static int ExtractDaysFromText(string text)
        {
            int total = 0;
            var lines = text.Split(Environment.NewLine);

            foreach (var line in lines)
            {
                var start = line.IndexOf("(");
                var end = line.IndexOf(")");
                if (start >= 0 && end > start)
                {
                    var numberPart = line.Substring(start + 1, end - start - 1);
                    if (int.TryParse(numberPart.Split(' ')[0], out int days))
                        total += days;
                }
            }

            return total;
        }

        

        private static Dictionary<int, string> BuildMonthlySummary(
            IEnumerable<Data.Models.Leave> leaves,
            int year)
        {
            var result = new Dictionary<int, string>();

            foreach (var leave in leaves)
            {
                var start = leave.StartDate;
                var end = leave.EndDate;

                if (start.Year < year)
                    start = new DateTime(year, 1, 1);

                if (end.Year > year)
                    end = new DateTime(year, 12, 31);

                var current = start;

                while (current <= end)
                {
                    int month = current.Month;

                    var monthStart = new DateTime(current.Year, current.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var rangeStart = current;
                    var rangeEnd = end < monthEnd ? end : monthEnd;

                    int days = (rangeEnd - rangeStart).Days + 1;

                    string text =
                        $"{rangeStart:dd}-{rangeEnd:dd} ({days}) Gün";

                    if (result.ContainsKey(month))
                        result[month] += Environment.NewLine + text;
                    else
                        result[month] = text;

                    current = rangeEnd.AddDays(1);
                }
            }

            return result;
        }

        private static int CalculateYearlyTotalDays(
            IEnumerable<Data.Models.Leave> leaves,
            int year)
        {
            int total = 0;

            foreach (var leave in leaves)
            {
                var start = leave.StartDate;
                var end = leave.EndDate;

                if (start.Year < year)
                    start = new DateTime(year, 1, 1);

                if (end.Year > year)
                    end = new DateTime(year, 12, 31);

                if (start <= end)
                    total += (end - start).Days + 1;
            }

            return total;
        }

        private int GetRemainingAnnualLeave(SqliteConnection connection, int employeeId, int year)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        SELECT annual_entitled, annual_used, annual_manual_adjust
        FROM LeaveBalances
        WHERE employee_id = @empId AND year = @year;
    ";

            cmd.Parameters.AddWithValue("@empId", employeeId);
            cmd.Parameters.AddWithValue("@year", year);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                int entitled = reader.GetInt32(0);
                int used = reader.GetInt32(1);
                int adjust = reader.GetInt32(2);

                return entitled + adjust - used;
            }

            return 0;
        }

        private static void WriteHeader(IXLWorksheet sheet, int year)
        {
            string[] headers =
            {
                "S. N.","ADI SOYAD","TOPLAM İZİN",
                "OCAK","ŞUBAT","MART","NİSAN","MAYIS","HAZİRAN",
                "TEMMUZ","AĞUSTOS","EYLÜL","EKİM","KASIM","ARALIK",
                $"{year} PLAN","KALAN"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = headers[i];
                sheet.Cell(1, i + 1).Style.Font.Bold = true;
            }
        }
    }
}