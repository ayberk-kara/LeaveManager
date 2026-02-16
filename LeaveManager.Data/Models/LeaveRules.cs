using LeaveManager.Data.Models;
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

    // ================= 1 =================
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

    // ================= 2 =================
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

    // ================= 3 =================
    public class MaxConsecutiveDaysRule : LeaveRule
    {
        public MaxConsecutiveDaysRule() : base("Maksimum Ardışık Gün Kuralı") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            int duration = (newLeave.EndDate - newLeave.StartDate).Days + 1;

            if (duration > 10)
            {
                reason = "Bir izin en fazla 10 gün ardışık olabilir.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    // ================= 4 =================
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

    // ================= 5 =================
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

    // ================= 6 =================
    public class SickLeaveLimitRule : LeaveRule
    {
        public SickLeaveLimitRule() : base("Yıllık Rapor İzni Limit Kuralı") { }

        public override bool Validate(Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            if (newLeave.Type != "Sick")
            {
                reason = string.Empty;
                return true;
            }

            int year = newLeave.StartDate.Year;

            int total = existingLeaves
                .Where(l => l.Type == "Sick" && l.StartDate.Year == year)
                .Sum(l => (l.EndDate - l.StartDate).Days + 1);

            total += (newLeave.EndDate - newLeave.StartDate).Days + 1;

            if (total > 40)
            {
                reason = "Bir takvim yılı içinde toplam raporlu izin süresi 40 günü aşamaz.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    // ================= 7 =================
    public class AnnualLeaveLimitRule : LeaveRule
    {
        public AnnualLeaveLimitRule() : base("Yıllık İzin Limit Kuralı") { }

        public override bool Validate(Employee employee,
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

            int year = newLeave.StartDate.Year;

            int total = existingLeaves
                .Where(l => l.Type == "Annual" && l.StartDate.Year == year)
                .Sum(l => (l.EndDate - l.StartDate).Days + 1);

            total += (newLeave.EndDate - newLeave.StartDate).Days + 1;

            if (total > 30)
            {
                reason = "Bir takvim yılı içinde toplam yıllık izin süresi 30 günü aşamaz.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    // ================= 8 =================
    public class AssistantConflictRule : LeaveRule
    {
        public AssistantConflictRule() : base("Aynı Müdür Yardımcısı Çakışma Kuralı") { }

        public override bool Validate(Employee employee,
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

    public static class LeaveRules
    {
        private static readonly List<LeaveRule> _rules = new()
        {
            new DateRangeRule(),
            new NoPastStartRule(),
            new MaxConsecutiveDaysRule(),
            new NoOverlapRule(),
            new OneLeavePerDayRule(),
            new SickLeaveLimitRule(),
            new AnnualLeaveLimitRule(),
            new AssistantConflictRule()
        };

        public static bool ValidateAll(
            Employee employee,
            IEnumerable<Employee> allEmployees,
            IEnumerable<Leave> existingLeaves,
            Leave newLeave,
            out string reason)
        {
            foreach (var rule in _rules)
            {
                if (!rule.Validate(employee, allEmployees, existingLeaves, newLeave, out reason))
                    return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}