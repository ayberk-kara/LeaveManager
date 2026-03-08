using ClosedXML.Excel;
using LeaveManager.App;
using LeaveManager.App.Services;
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

            
            var managerGroups = employees
                .Where(e => e.Role == EmployeeRole.Assistant)
                .OrderBy(e => e.FullName)
                .Select(my => new
                {
                    Manager = my,
                    Subordinates = employees
                        .Where(emp => emp.ManagerId == my.Id)
                        .OrderBy(emp => emp.FullName)
                        .ToList()
                })
                .ToList();

            foreach (var group in managerGroups)
            {
                var my = group.Manager;
                sheet.Cell(row, 1).Value = index++;
                sheet.Cell(row, 2).Value = my.FullName + " (M.Y.)";

                ApplyRowColor(sheet, row, managerColors[my.Id]);

               
                for (int month = 1; month <= 12; month++)
                {
                    sheet.Cell(row, month + 3).Style.Fill.BackgroundColor = managerColors[my.Id];
                }

                row++;

                
                foreach (var emp in group.Subordinates)
                {
                    sheet.Cell(row, 1).Value = index++;
                    sheet.Cell(row, 2).Value = emp.FullName;

                    
                    for (int month = 1; month <= 12; month++)
                    {
                        int? assignedMyId = emp.GetManagerForMonth(month, year); 
                        if (assignedMyId.HasValue)
                            sheet.Cell(row, month + 3).Style.Fill.BackgroundColor = managerColors[assignedMyId.Value];
                        else
                            sheet.Cell(row, month + 3).Style.Fill.BackgroundColor = XLColor.DarkRed;
                    }

                    row++;
                }
            }

            
            for (int r = 2; r < row; r++)
            {
                sheet.Cell(r, 16).Value = ""; 
                sheet.Cell(r, 17).Value = ""; 
            }

           
            sheet.Cell(row, 2).Value = "TOPLAM";
            sheet.Range(row, 1, row, 17).Style.Fill.BackgroundColor = XLColor.LightGray;
            sheet.Range(row, 1, row, 17).Style.Font.Bold = true;

            
            sheet.Columns().AdjustToContents();
            var tableRange = sheet.Range(1, 1, row, 17);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            workbook.SaveAs(dialog.FileName);
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
                XLColor.FromHtml("#FFF2CC")
            };

            var result = new Dictionary<int, XLColor>();
            int colorIndex = 0;

            foreach (var managerId in managerIds)
            {
                var color = palette[colorIndex % palette.Length];
                result[managerId] = color;
                colorIndex++;
            }

            return result;
        }

        private static void ApplyRowColor(IXLWorksheet sheet, int row, XLColor color)
        {
            sheet.Range(row, 1, row, 17).Style.Fill.BackgroundColor = color;
        }

        private static void WriteHeader(IXLWorksheet sheet, int year)
        {
            string[] headers =
            {
                "S. N.","ADI SOYAD",
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