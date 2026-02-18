using ClosedXML.Excel;
using LeaveManager.App;
using LeaveManager.Data.Repositories;
using LeaveManager.Data.Storage;
using LeaveManager.Services;
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

            foreach (var employee in employees)
            {
                sheet.Cell(row, 1).Value = index++;
                sheet.Cell(row, 2).Value = employee.FullName;

                var leaves = _leaveRepository.GetByEmployeeId(connection, employee.Id);

                var annualLeaves = leaves
                    .Where(l => l.Type.ToLower().Contains("yıllık")
                                && l.StartDate.Year == year)
                    .ToList();

                sheet.Cell(row, 3).Value = annualLeaves.Sum(l => l.Days);

                var monthly = BuildMonthlySummary(annualLeaves);

                for (int month = 1; month <= 12; month++)
                {
                    sheet.Cell(row, month + 3).Value =
                        monthly.ContainsKey(month)
                            ? monthly[month]
                            : string.Empty;

                    sheet.Cell(row, month + 3).Style.Alignment.WrapText = true;
                }

                int planned = annualLeaves.Sum(l => l.Days);

                sheet.Cell(row, 16).Value = planned;
                sheet.Cell(row, 17).Value = 0; // balance logic yoksa 0

                row++;
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);
        }

        private static void WriteHeader(IXLWorksheet sheet, int year)
        {
            string[] headers =
            {
                "SAYI","ADI SOYAD","TOPLAM İZİN",
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

        private static Dictionary<int, string> BuildMonthlySummary(IEnumerable<Data.Models.Leave> leaves)
        {
            var result = new Dictionary<int, string>();

            foreach (var leave in leaves)
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
    }
}