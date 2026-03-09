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

        public override bool Validate(
            Employee employee,
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

        public override bool Validate(
            Employee employee,
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

            if (newLeave.EndDate.Date < DateTime.Today)
            {
                reason = "İzin geçmiş tarihlerde olamaz.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    public class NoOverlapRule : LeaveRule
    {
        public NoOverlapRule() : base("Çakışan İzin Kuralı") { }

        public override bool Validate(
            Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            var leaves = existingLeaves as IList<Leave> ?? existingLeaves.ToList();

            bool overlap = leaves.Any(l =>
                newLeave.StartDate <= l.EndDate &&
                newLeave.EndDate >= l.StartDate);

            if (overlap)
            {
                reason = "Bu izin mevcut başka bir izinle çakışıyor.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

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
            if (!newLeave.Type.Equals("Annual", StringComparison.OrdinalIgnoreCase))
            {
                reason = string.Empty;
                return true;
            }

            DateTime start = newLeave.StartDate.Date;
            DateTime end = newLeave.EndDate.Date;

            var allAssignments = _employeeRepository.GetAllManagerAssignments();

            var assignmentLookup = allAssignments.ToLookup(a =>
                (a.EmployeeId, a.Year, a.Month));

            var employees = allEmployees as IList<Employee> ?? allEmployees.ToList();

            foreach (var day in EachDay(start, end))
            {
                var assignment = assignmentLookup[(employee.Id, day.Year, day.Month)]
                    .FirstOrDefault();

                if (assignment == null)
                {
                    reason = $"{day:dd.MM.yyyy} tarihinde çalışanın atanmış bir müdür yardımcısı yok.";
                    return false;
                }

                int managerId = assignment.ManagerId;

                var teamMembers = employees
                    .Where(e => e.Id != employee.Id)
                    .Where(e =>
                    {
                        var a = assignmentLookup[(e.Id, day.Year, day.Month)]
                            .FirstOrDefault();

                        return a != null && a.ManagerId == managerId;
                    })
                    .ToList();

                int count = teamMembers.Count(member =>
                    member.Leaves.Any(l =>
                        l.Type.Equals("Annual", StringComparison.OrdinalIgnoreCase) &&
                        day >= l.StartDate &&
                        day <= l.EndDate));

                if (count >= 2)
                {
                    reason = $"{day:dd.MM.yyyy} tarihinde aynı müdür yardımcısına bağlı en fazla 2 kişi izin alabilir.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private IEnumerable<DateTime> EachDay(DateTime start, DateTime end)
        {
            for (var day = start; day <= end; day = day.AddDays(1))
                yield return day;
        }
    }

    public class ManagerAssignmentExistsRule : LeaveRule
    {
        private readonly EmployeeRepository _employeeRepository = new();

        public ManagerAssignmentExistsRule()
            : base("Manager Ataması Kontrolü") { }

        public override bool Validate(
            Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            var assignments = _employeeRepository.GetAllManagerAssignments();

            var assignmentSet = assignments
                .Where(a => a.EmployeeId == employee.Id)
                .Select(a => (a.Year, a.Month))
                .ToHashSet();

            DateTime start = newLeave.StartDate.Date;
            DateTime end = newLeave.EndDate.Date;

            var months = new List<(int year, int month)>();

            var cursor = new DateTime(start.Year, start.Month, 1);

            while (cursor <= end)
            {
                months.Add((cursor.Year, cursor.Month));
                cursor = cursor.AddMonths(1);
            }

            foreach (var m in months)
            {
                if (!assignmentSet.Contains((m.year, m.month)))
                {
                    reason =
                        $"İzin eklenemiyor.\n\n" +
                        $"{m.month:D2}/{m.year} ayı için çalışan adına atanmış bir MY bulunamadı.\n\n" +
                        $"İzin girmeden önce ilgili ay için MY ataması yapılmalıdır.";

                    return false;
                }
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
            var leaves = existingLeaves as IList<Leave> ?? existingLeaves.ToList();

            int newDuration = (newLeave.EndDate - newLeave.StartDate).Days + 1;

            foreach (var leave in leaves)
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
                        reason = "5 günden uzun iki izin arasında en az 3 ay olmalıdır.";
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}