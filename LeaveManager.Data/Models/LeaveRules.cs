using LeaveManager.Data.Models;
using LeaveManager.Data.Repositories;
using LeaveManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LeaveManager.App
{
    public abstract class LeaveRule
    {
        public string RuleName { get; }

        protected LeaveRule(string ruleName)
        {
            RuleName = ruleName;
        }

        public abstract bool Validate(
            Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason);
    }

    public class DateRangeRule : LeaveRule
    {
        public DateRangeRule() : base("Geçersiz Tarih Aralığı") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (newLeave.EndDate < newLeave.StartDate)
            {
                reason = "İzin bitiş tarihi, başlangıç tarihinden küçük olamaz.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    public class NoPastStartRule : LeaveRule
    {
        public NoPastStartRule() : base("Geçmiş Tarihte İzin Başlatılamaz") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (newLeave.StartDate.Date < DateTime.Today)
            {
                reason = "İzin başlangıç tarihi bugünden önce olamaz.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    public class NoOverlapRule : LeaveRule
    {
        public NoOverlapRule() : base("Çakışan İzin Kuralı") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (existingLeaves.Any(l =>
                newLeave.StartDate <= l.EndDate &&
                newLeave.EndDate >= l.StartDate))
            {
                reason = "Bu izin, mevcut başka bir izinle tarih çakışması içeriyor.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    public class OneLeavePerDayRule : LeaveRule
    {
        public OneLeavePerDayRule() : base("Aynı Gün Tek İzin Kuralı") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (existingLeaves.Any(l => l.StartDate.Date == newLeave.StartDate.Date))
            {
                reason = "Aynı başlangıç tarihi için birden fazla izin girilemez.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    // updated rule  
    public class AssistantConflictRule : LeaveRule
    {
        private readonly EmployeeRepository _employeeRepository = new();

        public AssistantConflictRule()
            : base("Aynı Müdür Yardımcısı Çakışma Kuralı") { }

        public override bool Validate(
            Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (newLeave.Type != "Annual")
            {
                reason = string.Empty;
                return true;
            }

            DateTime current = newLeave.StartDate;

            while (current <= newLeave.EndDate)
            {
                var managerId = _employeeRepository.GetManagerIdForDate(employee.Id, current);

                if (managerId == null)
                {
                    reason = $"{current:dd.MM.yyyy} tarihinde çalışan herhangi bir müdür yardımcısına bağlı değil.";
                    return false;
                }

                var teamMembers = _employeeRepository
                    .GetEmployeesUnderManager(managerId.Value, current)
                    .Where(e => e.Id != employee.Id)
                    .ToList();

                int count = 0;

                foreach (var member in teamMembers)
                {
                    foreach (var leave in member.Leaves.Where(l => l.Type == "Annual"))
                    {
                        if (current >= leave.StartDate && current <= leave.EndDate)
                        {
                            count++;
                        }
                    }
                }

                if (count >= 2)
                {
                    reason = $"{current:dd.MM.yyyy} tarihinde aynı müdür yardımcısına bağlı en fazla 2 kişi izin alabilir.";
                    return false;
                }

                current = current.AddDays(1);
            }

            reason = string.Empty;
            return true;
        }
    }

    public class LongLeaveGapRule : LeaveRule
    {
        public LongLeaveGapRule() : base("Uzun İzinler Arası 3 Ay Kuralı") { }

        public override bool Validate(
            Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            int newDuration = (newLeave.EndDate - newLeave.StartDate).Days + 1;

            foreach (var leave in existingLeaves)
            {
                int existingDuration = (leave.EndDate - leave.StartDate).Days + 1;

                if (existingDuration > 5 && newDuration > 5)
                {
                    int gap;

                    if (newLeave.StartDate > leave.EndDate)
                        gap = (newLeave.StartDate - leave.EndDate).Days;
                    else if (leave.StartDate > newLeave.EndDate)
                        gap = (leave.StartDate - newLeave.EndDate).Days;
                    else
                        continue;

                    if (gap < 90)
                    {
                        reason = "5 günden uzun iki izin arasında en az 3 ay (90 gün) olmalıdır.";
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}