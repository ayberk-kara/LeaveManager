using System.Collections.Generic;
using LeaveManager.App;

namespace LeaveManager.App.Services
{
    public interface IExportService
    {
        public bool ExportAnnualPlanToExcel(IEnumerable<EmployeeItem> employees, int year);
    }
}