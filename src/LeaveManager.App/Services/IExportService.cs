using System.Collections.Generic;
using LeaveManager.Models;

namespace LeaveManager.Services
{
    public interface IExportService
    {
        void ExportAnnualPlanToExcel(IEnumerable<Employee> employees, int year);
    }
}