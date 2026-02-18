using System.Collections.Generic;
using LeaveManager.App;

namespace LeaveManager.App.Services
{
    public interface IExportService
    {
        void ExportAnnualPlanToExcel(IEnumerable<EmployeeItem> employees, int year);
    }
}