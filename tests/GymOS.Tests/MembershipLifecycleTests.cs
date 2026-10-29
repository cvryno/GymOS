using GymOS.API.Controllers;
using GymOS.Application.DTOs;
using GymOS.Application.Interfaces;
using GymOS.Domain.Entities;
using GymOS.Domain.Enums;
using GymOS.Infrastructure.Data;
using GymOS.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymOS.Tests;

public sealed class MembershipLifecycleTests
{
    [Fact]
    public void DemoSqlSeedContainsAValidPasswordHash()
    {
        const string hash = "$2a$11$LEONOsEQymWYUPpbMBUkoeCjhtbPA28p7Ql3Ha.OLxenQ9CZ.g0fW";
        Assert.True(BCrypt.Net.BCrypt.Verify("Admin@1234", hash));
    }

    [Fact]
    public void EcuadorCalendarUsesLocalMidnightForUtcRanges()
    {
        var calendar = CreateCalendar("2026-10-29T02:00:00Z");
        var (start, end) = calendar.DayRangeUtc(new DateOnly(2026, 10, 28));

        Assert.Equal(new DateOnly(2026, 10, 28), calendar.Today);
        Assert.Equal(new DateTime(2026, 10, 28, 5, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 10, 29, 5, 0, 0, DateTimeKind.Utc), end);
    }

    [Fact]
    public async Task UnpaidMembershipKeepsCurrentAccessAndCreatesFullBalanceInvoice()
    {
        await using var db = CreateDb();
        var (member, plan, existing) = SeedCurrentMembership(db);
        var calendar = CreateCalendar("2026-10-28T14:00:00Z");
        var controller = new MembershipsController(db, new FakeWhatsAppService(), calendar);

        await controller.Create(new CreateMembershipDto(member.Id, plan.Id, new DateTime(2026, 10, 28), 0, "Renovación"));

        var pending = await db.Memberships.SingleAsync(m => m.Status == MembershipStatus.Pending);
        var invoice = await db.Invoices.SingleAsync(i => i.MembershipId == pending.Id);
        Assert.Equal(MembershipStatus.Active, existing.Status);
        Assert.Equal(PaymentStatus.Pending, invoice.Status);
        Assert.Equal(plan.Price, invoice.Total);
        Assert.Null(invoice.PaidAt);
    }

    [Fact]
    public async Task PayingPendingMembershipStartsItTodayAndReplacesPreviousMembership()
    {
        await using var db = CreateDb();
        var (member, plan, existing) = SeedCurrentMembership(db);
        var calendar = CreateCalendar("2026-10-28T14:00:00Z");
        var memberships = new MembershipsController(db, new FakeWhatsAppService(), calendar);
        await memberships.Create(new CreateMembershipDto(member.Id, plan.Id, new DateTime(2026, 10, 20), 0, null));
        var invoice = await db.Invoices.SingleAsync(i => i.Status == PaymentStatus.Pending);

        var invoices = new InvoicesController(db, calendar);
        var response = await invoices.MarkAsPaid(invoice.Id, "Tarjeta");

        var renewed = await db.Memberships.SingleAsync(m => m.Id == invoice.MembershipId);
        Assert.IsType<NoContentResult>(response);
        Assert.Equal(MembershipStatus.Cancelled, existing.Status);
        Assert.Equal(MembershipStatus.Active, renewed.Status);
        Assert.Equal(calendar.StartOfDayUtc(calendar.Today), renewed.StartDate);
        Assert.Equal(calendar.StartOfDayUtc(calendar.Today.AddDays(plan.DurationDays)), renewed.EndDate);
        Assert.Equal(plan.Price, renewed.AmountPaid);
    }

    [Fact]
    public async Task PartialPaymentIsRejectedWithoutWritingMembershipOrInvoice()
    {
        await using var db = CreateDb();
        var (member, plan, _) = SeedCurrentMembership(db);
        var controller = new MembershipsController(db, new FakeWhatsAppService(), CreateCalendar("2026-10-28T14:00:00Z"));

        var response = await controller.Create(new CreateMembershipDto(member.Id, plan.Id, new DateTime(2026, 10, 28), 10, null));

        var result = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.NotNull(result.Value);
        Assert.Empty(await db.Memberships.Where(m => m.Status == MembershipStatus.Pending).ToListAsync());
        Assert.Empty(await db.Invoices.ToListAsync());
    }

    private static GymDbContext CreateDb() => new(new DbContextOptionsBuilder<GymDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static (Member Member, MembershipPlan Plan, Membership Existing) SeedCurrentMembership(GymDbContext db)
    {
        var member = new Member { FirstName = "Ana", LastName = "Pérez", Email = $"ana-{Guid.NewGuid():N}@test.local", PhoneNumber = "0990000000", Status = MemberStatus.Active };
        var plan = new MembershipPlan { Name = "Mensual", Price = 50m, DurationDays = 30, IsActive = true };
        var existingPlan = new MembershipPlan { Name = "Anterior", Price = 40m, DurationDays = 30, IsActive = true };
        var existing = new Membership
        {
            Member = member, Plan = existingPlan, StartDate = new DateTime(2026, 10, 1, 5, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 11, 1, 5, 0, 0, DateTimeKind.Utc), Status = MembershipStatus.Active, AmountPaid = 40m
        };
        db.AddRange(member, plan, existingPlan, existing);
        db.SaveChanges();
        return (member, plan, existing);
    }

    private static GymCalendar CreateCalendar(string utcNow)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Gym:TimeZone"] = "America/Guayaquil"
        }).Build();
        return new GymCalendar(configuration, new FixedTimeProvider(DateTimeOffset.Parse(utcNow)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeWhatsAppService : IWhatsAppService
    {
        public Task<bool> SendMembershipExpiryNotificationAsync(Member member, Membership membership) => Task.FromResult(true);
        public Task<bool> SendCustomMessageAsync(string phoneNumber, string message) => Task.FromResult(true);
    }
}
