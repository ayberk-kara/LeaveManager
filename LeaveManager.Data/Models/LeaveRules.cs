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
            bool overlap = existingLeaves.Any(l =>
                newLeave.StartDate <= l.EndDate &&
                newLeave.EndDate >= l.StartDate);

            if (overlap)
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

        public override bool Validate(
            Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            bool overlap = existingLeaves.Any(l =>
                newLeave.StartDate <= l.EndDate &&
                newLeave.EndDate >= l.StartDate);

            if (overlap)
            {
                reason = "Çalışan aynı tarihlerde birden fazla izin kullanamaz.";
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

            var assignments = _employeeRepository
                .GetManagerAssignments(employee.Id)
                .Where(a => a.StartDate <= end && a.EndDate >= start)
                .ToList();

            if (!assignments.Any())
            {
                reason = "Çalışan izin tarihleri içinde herhangi bir müdür yardımcısına bağlı değil.";
                return false;
            }

            foreach (var assignment in assignments)
            {
                DateTime segmentStart = assignment.StartDate > start ? assignment.StartDate : start;
                DateTime segmentEnd = assignment.EndDate < end ? assignment.EndDate : end;

                var teamMembers = allEmployees
                    .Where(e => e.Id != employee.Id)
                    .Where(e =>
                    {
                        var a = _employeeRepository.GetManagerAssignments(e.Id)
                            .FirstOrDefault(x =>
                                x.ManagerId == assignment.ManagerId &&
                                x.StartDate <= segmentEnd &&
                                x.EndDate >= segmentStart);

                        return a != null;
                    })
                    .ToList();

                foreach (var day in EachDay(segmentStart, segmentEnd))
                {
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
