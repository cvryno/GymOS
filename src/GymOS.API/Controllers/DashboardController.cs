using GymOS.Application.DTOs;
using GymOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly GymDbContext _db;

    public DashboardController(GymDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var totalMembers = await _db.Members.CountAsync();
        var activeMembers = await _db.Members.CountAsync(m => m.Status == Domain.Enums.MemberStatus.Active);
        var newMembersThisMonth = await _db.Members.CountAsync(m => m.CreatedAt >= thisMonthStart);

        var revenueThisMonth = await _db.Invoices
            .Where(i => i.PaidAt >= thisMonthStart && i.Status == Domain.Enums.PaymentStatus.Paid)
            .SumAsync(i => (decimal?)i.Total) ?? 0;

        var revenueLastMonth = await _db.Invoices
            .Where(i => i.PaidAt >= lastMonthStart && i.PaidAt < thisMonthStart && i.Status == Domain.Enums.PaymentStatus.Paid)
            .SumAsync(i => (decimal?)i.Total) ?? 0;

        var totalClasses = await _db.GymClasses.CountAsync();
        var today = now.DayOfWeek;
        var classesToday = await _db.ClassSchedules.CountAsync(s => s.DayOfWeek == today && s.IsActive);

        var expiringIn3 = await _db.Memberships
            .CountAsync(m => m.Status == Domain.Enums.MembershipStatus.Active &&
                m.EndDate.Date == now.Date.AddDays(3));

        var expiringIn7 = await _db.Memberships
            .CountAsync(m => m.Status == Domain.Enums.MembershipStatus.Active &&
                m.EndDate <= now.AddDays(7) && m.EndDate >= now);

        var activeMembershipsCount = await _db.Memberships
            .CountAsync(m => m.Status == Domain.Enums.MembershipStatus.Active);

        // Monthly revenue (last 6 months)
        var monthlyRevenue = new List<MonthlyRevenueDto>();
        for (int i = 5; i >= 0; i--)
        {
            var start = thisMonthStart.AddMonths(-i);
            var end = start.AddMonths(1);
            var rev = await _db.Invoices
                .Where(inv => inv.PaidAt >= start && inv.PaidAt < end && inv.Status == Domain.Enums.PaymentStatus.Paid)
                .SumAsync(inv => (decimal?)inv.Total) ?? 0;
            monthlyRevenue.Add(new MonthlyRevenueDto(start.ToString("MMM yyyy"), rev));
        }

        // Memberships by plan
        var membershipsByPlan = await _db.Memberships
            .Include(m => m.Plan)
            .Where(m => m.Status == Domain.Enums.MembershipStatus.Active)
            .GroupBy(m => m.Plan.Name)
            .Select(g => new MembershipsByPlanDto(g.Key, g.Count(), "#6366f1"))
            .ToListAsync();

        var colors = new[] { "#6366f1", "#06b6d4", "#10b981", "#f59e0b", "#ef4444" };
        membershipsByPlan = membershipsByPlan.Select((p, i) =>
            new MembershipsByPlanDto(p.PlanName, p.Count, colors[i % colors.Length])).ToList();

        // Top classes
        var topClasses = await _db.ClassEnrollments
            .Include(ce => ce.GymClass)
            .Where(ce => ce.IsActive)
            .GroupBy(ce => ce.GymClass.Name)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new TopClassDto(g.Key, g.Count()))
            .ToListAsync();

        // Recent members
        var recentMembers = await _db.Members
            .Include(m => m.Memberships.Where(ms => ms.Status == Domain.Enums.MembershipStatus.Active))
                .ThenInclude(ms => ms.Plan)
            .OrderByDescending(m => m.CreatedAt)
            .Take(5)
            .Select(m => new MemberDto(
                m.Id, m.FirstName, m.LastName, m.Email, m.PhoneNumber,
                m.PhotoUrl, m.Status.ToString(), m.DateOfBirth, m.Address,
                m.EmergencyContactName, m.EmergencyContactPhone, m.CreatedAt,
                m.Memberships.Any() ? new ActiveMembershipDto(
                    m.Memberships.First().Id,
                    m.Memberships.First().Plan.Name,
                    m.Memberships.First().StartDate,
                    m.Memberships.First().EndDate,
                    m.Memberships.First().Status.ToString(),
                    m.Memberships.First().DaysRemaining) : null))
            .ToListAsync();

        // Expiring memberships (next 7 days)
        var expiringMemberships = await _db.Memberships
            .Include(m => m.Member)
            .Include(m => m.Plan)
            .Where(m => m.Status == Domain.Enums.MembershipStatus.Active &&
                m.EndDate <= now.AddDays(7) && m.EndDate >= now)
            .OrderBy(m => m.EndDate)
            .Take(10)
            .Select(m => new MembershipDto(
                m.Id, m.MemberId, m.Member.FullName, m.Member.Email, m.Member.PhoneNumber,
                m.PlanId, m.Plan.Name, m.StartDate, m.EndDate,
                m.Status.ToString(), m.AmountPaid, m.DaysRemaining, m.NotificationSent))
            .ToListAsync();

        return Ok(new DashboardStatsDto(
            totalMembers, activeMembers, newMembersThisMonth,
            revenueThisMonth, revenueLastMonth,
            totalClasses, classesToday,
            expiringIn3, expiringIn7, activeMembershipsCount,
            monthlyRevenue, membershipsByPlan, topClasses,
            recentMembers, expiringMemberships));
    }

    [HttpGet("reports/attendance")]
    public async Task<IActionResult> AttendanceReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var start = DateTime.SpecifyKind((from ?? DateTime.UtcNow.Date.AddDays(-29)).Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind((to ?? DateTime.UtcNow.Date).Date.AddDays(1), DateTimeKind.Utc);
        if (end <= start || (end - start).TotalDays > 366)
            return BadRequest(new { message = "El rango debe ser válido y no exceder 366 días" });
        var report = await _db.AccessLogs.Where(a => a.CheckIn >= start && a.CheckIn < end)
            .GroupBy(a => a.CheckIn.Date)
            .Select(g => new { date = g.Key, visits = g.Count(), distinctMembers = g.Select(x => x.MemberId).Distinct().Count() })
            .OrderBy(x => x.date).ToListAsync();
        return Ok(report);
    }
}
