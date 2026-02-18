using ClosedXML.Excel;
using LeaveManager.Models;
using LeaveManager.Data.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LeaveManager.Services
{
    public sealed class ExcelExportService : IExportService
    {
        public void ExportAnnualPlanToExcel(IEnumerable<Employee> employees, int year)
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

            WriteHeader(sheet);

            int row = 2;
            int index = 1;

            foreach (var employee in employees.Where(e => e.IsActive))
            {
                sheet.Cell(row, 1).Value = index++;
                sheet.Cell(row, 2).Value = employee.FullName;
                sheet.Cell(row, 3).Value = employee.AnnualLeaveBalance;

                var monthlyData = BuildMonthlySummary(employee, year);

                for (int month = 1; month <= 12; month++)
                {
                    sheet.Cell(row, month + 3).Value =
                        monthlyData.ContainsKey(month)
                            ? monthlyData[month]
                            : string.Empty;

                    sheet.Cell(row, month + 3).Style.Alignment.WrapText = true;
                }

                sheet.Cell(row, 16).Value = CalculatePlannedAnnual(employee, year);
                sheet.Cell(row, 17).Value = employee.AnnualLeaveBalance;

                row++;
            }

            sheet.Columns().AdjustToContents();

            workbook.SaveAs(dialog.FileName);
        }

        private static void WriteHeader(IXLWorksheet sheet)
        {
            string[] headers =
            {
                "SAYI","ADI SOYAD","TOPLAM İZİN",
                "OCAK","ŞUBAT","MART","NİSAN","MAYIS","HAZİRAN",
                "TEMMUZ","AĞUSTOS","EYLÜL","EKİM","KASIM","ARALIK",
                "2026 PLAN","KALAN"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = headers[i];
                sheet.Cell(1, i + 1).Style.Font.Bold = true;
            }
        }

        private static Dictionary<int, string> BuildMonthlySummary(Employee employee, int year)
        {
            var result = new Dictionary<int, string>();

            var annualLeaves = employee.Leaves
                .Where(l => l.Type.ToLower().Contains("yıllık")
                            && l.StartDate.Year == year);

            foreach (var leave in annualLeaves)
            {
                int month = leave.StartDate.Month;

                string text =
                    $"{leave.StartDate:dd}-{leave.EndDate:dd} ({leave.Days}) Gün";

                if (result.ContainsKey(month))
                    result[month] += Environment.NewLine + text;
                else
                    result[month] = text;
            }

            return result;
        }

        private static int CalculatePlannedAnnual(Employee employee, int year)
        {
            return employee.Leaves
                .Where(l => l.Type.ToLower().Contains("yıllık")
                            && l.StartDate.Year == year)
                .Sum(l => l.Days);
        }
    }
}