using ClosedXML.Excel;
using LeaveManager.App;
using LeaveManager.Data.Repositories;
using LeaveManager.Data.Storage;
using LeaveManager.Models;
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

        public bool ExportAnnualPlanToExcel(IEnumerable<EmployeeItem> employees, int year)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Izin_Plani_{year}.xlsx"
            };
            if (dialog.ShowDialog() != true) return false;

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("İzin Planı");

            WriteHeader(sheet, year);

            int row = 2;
            int index = 1;

            var employeeRepository = new EmployeeRepository();
            var managerColors = GenerateManagerColors(employees);

            var managers = employees
                .Where(e => e.Role == EmployeeRole.Assistant)
                .OrderBy(e => e.FullName)
                .ToList();

            var personnel = employees
                .Where(e => e.Role != EmployeeRole.Assistant)
                .OrderBy(e => e.FullName)
                .ToList();

            var connectionString = $"Data Source={DbPaths.GetDbFilePath()}";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            int[] monthlyTotals = new int[12];
            int grandPlanned = 0;
            int grandRemaining = 0;

            
            foreach (var my in managers)
            {
                sheet.Cell(row, 1).Value = index++;
                sheet.Cell(row, 2).Value = my.FullName + " (M.Y.)";

                for (int month = 1; month <= 12; month++)
                {
                    sheet.Cell(row, month + 2).Style.Fill.BackgroundColor = managerColors[my.Id];
                }

                
                var leaves = _leaveRepository.GetByEmployeeId(connection, my.Id);
                var annualLeaves = leaves
                    .Where(l => l.Type.ToLower().Contains("yıllık") &&
                                (l.StartDate.Year <= year && l.EndDate.Year >= year))
                    .ToList();

                int yearlyPlanned = CalculateYearlyTotalDays(annualLeaves, year);
                int remaining = GetRemainingAnnualLeave(connection, my.Id, year);

                sheet.Cell(row, 15).Value = yearlyPlanned;
                sheet.Cell(row, 15).Style.Fill.BackgroundColor = XLColor.LightGray;

                sheet.Cell(row, 16).Value = remaining;
                sheet.Cell(row, 16).Style.Fill.BackgroundColor = XLColor.LightGray;

                
                var monthly = BuildMonthlySummary(annualLeaves, year);
                for (int month = 1; month <= 12; month++)
                {
                    if (monthly.ContainsKey(month))
                        sheet.Cell(row, month + 2).Value = monthly[month];

                    
                    if (monthly.ContainsKey(month))
                        monthlyTotals[month - 1] += ExtractDaysFromText(monthly[month]);
                }

                grandPlanned += yearlyPlanned;
                grandRemaining += remaining;

                row++;
            }

            
            foreach (var emp in personnel)
            {
                sheet.Cell(row, 1).Value = index++;
                sheet.Cell(row, 2).Value = emp.FullName;

                var leaves = _leaveRepository.GetByEmployeeId(connection, emp.Id);
                var annualLeaves = leaves
                    .Where(l => l.Type.ToLower().Contains("yıllık") &&
                                (l.StartDate.Year <= year && l.EndDate.Year >= year))
                    .ToList();

                int yearlyPlanned = CalculateYearlyTotalDays(annualLeaves, year);
                int remaining = GetRemainingAnnualLeave(connection, emp.Id, year);

                var monthly = BuildMonthlySummary(annualLeaves, year);

                for (int month = 1; month <= 12; month++)
                {
                    if (monthly.ContainsKey(month))
                        sheet.Cell(row, month + 2).Value = monthly[month];

                    int? assignedMyId = employeeRepository.GetManagerIdForDate(emp.Id, new DateTime(year, month, 1));
                    if (assignedMyId.HasValue && managerColors.ContainsKey(assignedMyId.Value))
                        sheet.Cell(row, month + 2).Style.Fill.BackgroundColor = managerColors[assignedMyId.Value];
                    else
                        sheet.Cell(row, month + 2).Style.Fill.BackgroundColor = XLColor.DarkRed;

                    sheet.Cell(row, month + 2).Style.Alignment.WrapText = true;

                    if (monthly.ContainsKey(month))
                        monthlyTotals[month - 1] += ExtractDaysFromText(monthly[month]);
                }

                // Plan ve Kalan
                sheet.Cell(row, 15).Value = yearlyPlanned;
                sheet.Cell(row, 15).Style.Fill.BackgroundColor = XLColor.LightGray;

                sheet.Cell(row, 16).Value = remaining;
                sheet.Cell(row, 16).Style.Fill.BackgroundColor = XLColor.LightGray;

                grandPlanned += yearlyPlanned;
                grandRemaining += remaining;

                row++;
            }

             
            sheet.Cell(row, 2).Value = "TOPLAM";
            sheet.Cell(row, 2).Style.Font.Bold = true;

            for (int i = 0; i < 12; i++)
                sheet.Cell(row, i + 3).Value = monthlyTotals[i];

            sheet.Cell(row, 15).Value = grandPlanned;
            sheet.Cell(row, 16).Value = grandRemaining;

            sheet.Range(row, 1, row, 16).Style.Font.Bold = true;
            sheet.Range(row, 1, row, 16).Style.Fill.BackgroundColor = XLColor.LightGray;


             
            sheet.Column(2).Width = sheet.Column(2).Width * 3;

             
            foreach (var col in sheet.ColumnsUsed()
                         .Where(c => c.ColumnNumber() != 1 && c.ColumnNumber() != 2 && c.ColumnNumber() != 15 && c.ColumnNumber() != 16))
            {
                col.Width = col.Width * 2;
            }


            foreach (var xLRow in sheet.RowsUsed())
            {
                xLRow.Height = xLRow.Height * 2;
            }

            sheet.Cells().Style.Font.FontName = "Times New Roman";
            sheet.Cells().Style.Font.FontSize = 12;


            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;      
            var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 1; 

            var totalUsedRange = sheet.Range(1, 1, sheet.LastRowUsed().RowNumber(), sheet.LastColumnUsed().ColumnNumber());
            totalUsedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            totalUsedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            workbook.SaveAs(dialog.FileName);
            return true;
        }

        private static Dictionary<int, XLColor> GenerateManagerColors(IEnumerable<EmployeeItem> employees)
        {
            var managerIds = employees
                .Where(e => e.Role == EmployeeRole.Assistant)
                .Select(e => e.Id)
                .ToList();

            
            var palette = new[]
            {
        XLColor.FromHtml("#D9EAD3"),
        XLColor.FromHtml("#CFE2F3"),
        XLColor.FromHtml("#FCE5CD"),
        XLColor.FromHtml("#EAD1DC"),
        XLColor.FromHtml("#FFF2CC"),
        XLColor.FromHtml("#F4CCCC"),
        XLColor.FromHtml("#D0E0E3"),
        XLColor.FromHtml("#F9CB9C")
    };

            var result = new Dictionary<int, XLColor>();
            int colorIndex = 0;

            foreach (var managerId in managerIds)
            {
                result[managerId] = palette[colorIndex % palette.Length];
                colorIndex++;
            }

            return result;
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

        private static Dictionary<int, string> BuildMonthlySummary(IEnumerable<Data.Models.Leave> leaves, int year)
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
                    string text = $"{rangeStart:dd}-{rangeEnd:dd} ({days}) Gün";

                    if (result.ContainsKey(month))
                        result[month] += Environment.NewLine + text;
                    else
                        result[month] = text;

                    current = rangeEnd.AddDays(1);
                }
            }
            return result;
        }

        private static int CalculateYearlyTotalDays(IEnumerable<Data.Models.Leave> leaves, int year)
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
                int manual = reader.GetInt32(2);

                return entitled - (used + manual); // 🔥 FIX
            }
            return 0;
        }

        private static void WriteHeader(IXLWorksheet sheet, int year)
        {
            string[] headers =
            {
                "S. N.","ADI SOYAD",
                "OCAK","ŞUBAT","MART","NİSAN","MAYIS","HAZİRAN",
                "TEMMUZ","AĞUSTOS","EYLÜL","EKİM","KASIM","ARALIK",
                $"{year}","KALAN"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = headers[i];
                sheet.Cell(1, i + 1).Style.Font.Bold = true;
            }
        }
    }
}