using GymOS.Application.DTOs;
using GymOS.Application.Interfaces;
using GymOS.Domain.Entities;
using GymOS.Domain.Enums;
using GymOS.Infrastructure.Data;
using GymOS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembershipsController : ControllerBase
{
    private readonly GymDbContext _db;
    private readonly IWhatsAppService _whatsApp;
    private readonly GymCalendar _calendar;

    public MembershipsController(GymDbContext db, IWhatsAppService whatsApp, GymCalendar calendar)
    {
        _db = db;
        _whatsApp = whatsApp;
        _calendar = calendar;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MembershipDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Memberships
            .Include(m => m.Member)
            .Include(m => m.Plan)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<MembershipStatus>(status, true, out var ms))
            query = query.Where(m => m.Status == ms);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(m =>
                m.Member.FirstName.Contains(search) ||
                m.Member.LastName.Contains(search) ||
                m.Member.Email.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MembershipDto(
                m.Id, m.MemberId, m.Member.FullName, m.Member.Email, m.Member.PhoneNumber,
                m.PlanId, m.Plan.Name, m.StartDate, m.EndDate,
                m.Status.ToString(), m.AmountPaid, m.DaysRemaining, m.NotificationSent))
            .ToListAsync();

        return Ok(new PagedResult<MembershipDto>(items, total, page, pageSize));
    }

    [HttpGet("expiring")]
    public async Task<ActionResult<List<MembershipDto>>> GetExpiring([FromQuery] int days = 7)
    {
        if (days < 0 || days > 90)
            return BadRequest(new { message = "El rango debe estar entre 0 y 90 días" });
        var today = _calendar.Today;
        var from = _calendar.StartOfDayUtc(today);
        var until = _calendar.StartOfDayUtc(today.AddDays(days + 1));
        var items = await _db.Memberships
            .Include(m => m.Member)
            .Include(m => m.Plan)
            .Where(m => m.Status == MembershipStatus.Active && m.EndDate > from && m.EndDate <= until)
            .OrderBy(m => m.EndDate)
            .Select(m => new MembershipDto(
                m.Id, m.MemberId, m.Member.FullName, m.Member.Email, m.Member.PhoneNumber,
                m.PlanId, m.Plan.Name, m.StartDate, m.EndDate,
                m.Status.ToString(), m.AmountPaid, m.DaysRemaining, m.NotificationSent))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<MembershipDto>> Create([FromBody] CreateMembershipDto dto)
    {
        if (dto.StartDate == default || dto.AmountPaid < 0)
            return BadRequest(new { message = "Fecha de inicio e importe deben ser válidos" });

        var member = await _db.Members.FindAsync(dto.MemberId);
        if (member is null) return BadRequest(new { message = "Miembro no encontrado" });

        var plan = await _db.MembershipPlans.FindAsync(dto.PlanId);
        if (plan is null) return BadRequest(new { message = "Plan no encontrado" });
        if (!plan.IsActive) return BadRequest(new { message = "El plan no está activo" });
        if (plan.DurationDays <= 0 || plan.Price <= 0)
            return BadRequest(new { message = "El plan tiene un precio o duración inválidos" });
        if (dto.AmountPaid != 0 && dto.AmountPaid != plan.Price)
            return BadRequest(new { message = "Para esta versión se debe registrar el pago completo o dejar la factura pendiente" });

        var startDate = DateOnly.FromDateTime(dto.StartDate);
        if (startDate > _calendar.Today)
            return BadRequest(new { message = "La fecha de inicio no puede estar en el futuro" });

        var membership = new Membership
        {
            MemberId = dto.MemberId,
            PlanId = dto.PlanId,
            StartDate = _calendar.StartOfDayUtc(startDate),
            EndDate = _calendar.StartOfDayUtc(startDate.AddDays(plan.DurationDays)),
            AmountPaid = dto.AmountPaid,
            Status = dto.AmountPaid == plan.Price ? MembershipStatus.Active : MembershipStatus.Pending,
            Notes = dto.Notes
        };

        if (membership.Status == MembershipStatus.Active)
        {
            var existingActive = await _db.Memberships
                .Where(m => m.MemberId == dto.MemberId && m.Status == MembershipStatus.Active)
                .ToListAsync();
            existingActive.ForEach(m => m.Status = MembershipStatus.Cancelled);
            member.Status = MemberStatus.Active;
        }
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{_calendar.Today:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}",
            MemberId = dto.MemberId,
            Subtotal = plan.Price,
            Total = plan.Price,
            DueDate = _calendar.StartOfDayUtc(startDate),
            PaidAt = membership.Status == MembershipStatus.Active ? _calendar.UtcNow : null,
            Status = membership.Status == MembershipStatus.Active ? PaymentStatus.Paid : PaymentStatus.Pending,
            PaymentMethod = membership.Status == MembershipStatus.Active ? "Efectivo" : null,
            Notes = dto.Notes,
            Membership = membership
        };
        _db.Memberships.Add(membership);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = membership.Id },
            new MembershipDto(membership.Id, membership.MemberId, member.FullName, member.Email, member.PhoneNumber,
                membership.PlanId, plan.Name, membership.StartDate, membership.EndDate,
                membership.Status.ToString(), membership.AmountPaid, membership.DaysRemaining, membership.NotificationSent));
    }

    [HttpPost("{id}/notify-whatsapp")]
    public async Task<IActionResult> SendWhatsAppNotification(int id)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["WhatsApp:AccessToken"]) ||
            string.IsNullOrWhiteSpace(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["WhatsApp:PhoneNumberId"]))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Las notificaciones de WhatsApp no están configuradas" });

        var membership = await _db.Memberships
            .Include(m => m.Member)
            .Include(m => m.Plan)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (membership is null) return NotFound(new { message = "Membresía no encontrada" });

        var success = await _whatsApp.SendMembershipExpiryNotificationAsync(membership.Member, membership);

        return success
            ? Ok(new { message = "Notificación enviada exitosamente" })
            : BadRequest(new { message = "Error al enviar la notificación" });
    }

    [HttpPut("{id}/cancel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Cancel(int id)
    {
        var membership = await _db.Memberships.FindAsync(id);
        if (membership is null) return NotFound();

        membership.Status = MembershipStatus.Cancelled;
        membership.UpdatedAt = _calendar.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Plans CRUD
    [HttpGet("plans")]
    public async Task<ActionResult<List<MembershipPlanDto>>> GetPlans()
    {
        var plans = await _db.MembershipPlans
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Price)
            .Select(p => new MembershipPlanDto(
                p.Id, p.Name, p.Description, p.Price,
                p.DurationDays, p.IsActive, p.IncludesClasses, p.MaxClassesPerMonth))
            .ToListAsync();
        return Ok(plans);
    }

    [HttpPost("plans")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<MembershipPlanDto>> CreatePlan([FromBody] CreateMembershipPlanDto dto)
    {
        var plan = new MembershipPlan
        {
            Name = dto.Name, Description = dto.Description,
            Price = dto.Price, DurationDays = dto.DurationDays,
            IncludesClasses = dto.IncludesClasses,
            MaxClassesPerMonth = dto.MaxClassesPerMonth, IsActive = true
        };
        _db.MembershipPlans.Add(plan);
        await _db.SaveChangesAsync();
        return Ok(new MembershipPlanDto(plan.Id, plan.Name, plan.Description, plan.Price,
            plan.DurationDays, plan.IsActive, plan.IncludesClasses, plan.MaxClassesPerMonth));
    }

    [HttpPut("plans/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] CreateMembershipPlanDto dto)
    {
        var plan = await _db.MembershipPlans.FindAsync(id);
        if (plan is null) return NotFound();

        plan.Name = dto.Name; plan.Description = dto.Description;
        plan.Price = dto.Price; plan.DurationDays = dto.DurationDays;
        plan.IncludesClasses = dto.IncludesClasses;
        plan.MaxClassesPerMonth = dto.MaxClassesPerMonth;
        plan.UpdatedAt = _calendar.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
