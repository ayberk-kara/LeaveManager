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

    public class AssistantConflictRule : LeaveRule
    {
        public AssistantConflictRule()
            : base("Aynı Müdür Yardımcısı Çakışma Kuralı") { }

        public override bool Validate(
            Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (newLeave.Type != "Annual" || employee.ManagerId == null)
            {
                reason = string.Empty;
                return true;
            }

            var sameAssistantEmployees = allEmployees
                .Where(e => e.ManagerId == employee.ManagerId && e.Id != employee.Id);

            foreach (var other in sameAssistantEmployees)
            {
                foreach (var leave in other.Leaves.Where(l => l.Type == "Annual"))
                {
                    if (newLeave.StartDate <= leave.EndDate &&
                        newLeave.EndDate >= leave.StartDate)
                    {
                        reason = "Aynı müdür yardımcısına bağlı iki çalışan aynı tarihlerde yıllık izin kullanamaz.";
                        return false;
                    }
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