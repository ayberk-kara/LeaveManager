using LeaveManager.App;
using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;
using LeaveManager.Data.Storage;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace LeaveManager.Business
{
    public class LeaveService
    {
        private readonly LeaveRepository _leaveRepository;
        private readonly EmployeeRepository _employeeRepository;
        private readonly List<LeaveRule> _rules;

        private string ConnectionString =>
            $"Data Source={DbPaths.GetDbFilePath()}";

        public LeaveService()
        {
            _leaveRepository = new LeaveRepository();
            _employeeRepository = new EmployeeRepository();

            _rules = new List<LeaveRule>
            {
                new DateRangeRule(),
                // new NoPastStartRule(),
                new NoOverlapRule(),
                new LongLeaveGapRule(),
                
            };
        }

        public bool TryAddLeave(Leave newLeave, out string errorMessage)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var tx = connection.BeginTransaction();

            try
            {
                var employee = _employeeRepository.GetById(newLeave.EmployeeId);

                if (employee == null)
                {
                    errorMessage = "Çalışan bulunamadı.";
                    return false;
                }

                var existingLeaves =
                    _leaveRepository.GetByEmployeeId(connection, newLeave.EmployeeId);

                var allEmployees = _employeeRepository.GetAllActive();

                foreach (var rule in _rules)
                {
                    if (!rule.Validate(employee, allEmployees, existingLeaves, newLeave, out errorMessage))
                        return false;
                }

                _leaveRepository.Add(connection, tx, newLeave);

                tx.Commit();

                errorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}