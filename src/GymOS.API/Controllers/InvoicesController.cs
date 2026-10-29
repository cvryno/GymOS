using GymOS.Application.DTOs;
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
public class InvoicesController : ControllerBase
{
    private readonly GymDbContext _db;
    private readonly GymCalendar _calendar;

    public InvoicesController(GymDbContext db, GymCalendar calendar) { _db = db; _calendar = calendar; }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Invoices.Include(i => i.Member).AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PaymentStatus>(status, true, out var ps))
            query = query.Where(i => i.Status == ps);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(i =>
                i.InvoiceNumber.Contains(search) ||
                i.Member.FirstName.Contains(search) ||
                i.Member.LastName.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new InvoiceDto(
                i.Id, i.InvoiceNumber, i.MemberId, i.Member.FullName,
                i.Subtotal, i.Discount, i.Tax, i.Total,
                i.Status.ToString(), i.DueDate, i.PaidAt, i.PaymentMethod))
            .ToListAsync();

        return Ok(new PagedResult<InvoiceDto>(items, total, page, pageSize));
    }

    [HttpPut("{id}/mark-paid")]
    [Authorize(Policy = "AdminOrReceptionist")]
    public async Task<IActionResult> MarkAsPaid(int id, [FromQuery] string paymentMethod = "Efectivo")
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice is null) return NotFound();
        if (invoice.Status == PaymentStatus.Paid)
            return BadRequest(new { message = "La factura ya está pagada" });
        if (string.IsNullOrWhiteSpace(paymentMethod) || paymentMethod.Length > 50)
            return BadRequest(new { message = "Método de pago inválido" });
        invoice.Status = PaymentStatus.Paid;
        invoice.PaidAt = _calendar.UtcNow;
        invoice.PaymentMethod = paymentMethod;
        invoice.UpdatedAt = _calendar.UtcNow;
        if (invoice.MembershipId.HasValue)
        {
            var membership = await _db.Memberships.Include(m => m.Member).Include(m => m.Plan)
                .FirstOrDefaultAsync(m => m.Id == invoice.MembershipId.Value);
            if (membership is not null)
            {
                if (membership.Status != MembershipStatus.Pending)
                    return BadRequest(new { message = "La membresía asociada ya no está pendiente de pago" });
                membership.Status = MembershipStatus.Active;
                membership.AmountPaid += invoice.Total;
                membership.StartDate = _calendar.StartOfDayUtc(_calendar.Today);
                membership.EndDate = _calendar.StartOfDayUtc(_calendar.Today.AddDays(membership.Plan.DurationDays));
                membership.UpdatedAt = _calendar.UtcNow;
                membership.Member.Status = GymOS.Domain.Enums.MemberStatus.Active;
                var previous = await _db.Memberships.Where(m => m.MemberId == membership.MemberId &&
                    m.Id != membership.Id && m.Status == MembershipStatus.Active).ToListAsync();
                previous.ForEach(m => m.Status = MembershipStatus.Cancelled);
            }
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/refund")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Refund(int id)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice is null) return NotFound();
        if (invoice.Status != PaymentStatus.Paid)
            return BadRequest(new { message = "Solo se pueden reembolsar facturas pagadas" });
        invoice.Status = PaymentStatus.Refunded;
        invoice.UpdatedAt = DateTime.UtcNow;
        if (invoice.MembershipId.HasValue)
        {
            var membership = await _db.Memberships.FindAsync(invoice.MembershipId.Value);
            if (membership is not null) { membership.Status = MembershipStatus.Cancelled; membership.UpdatedAt = _calendar.UtcNow; }
        }
        if (invoice.MembershipId.HasValue)
        {
            var memberId = invoice.MemberId;
            var anotherActiveMembership = await _db.Memberships.AnyAsync(m => m.MemberId == memberId &&
                m.Id != invoice.MembershipId && m.Status == MembershipStatus.Active && m.EndDate > _calendar.UtcNow);
            if (!anotherActiveMembership)
            {
                var member = await _db.Members.FindAsync(memberId);
                if (member is not null) member.Status = GymOS.Domain.Enums.MemberStatus.Inactive;
            }
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost]
    [Authorize(Policy = "AdminOrReceptionist")]
    public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceDto dto)
    {
        if (dto.Subtotal < 0 || dto.Discount < 0 || dto.Tax < 0 || dto.Discount > dto.Subtotal || dto.DueDate == default)
            return BadRequest(new { message = "Importes o fecha de vencimiento inválidos" });
        var member = await _db.Members.FindAsync(dto.MemberId);
        if (member is null) return BadRequest(new { message = "Miembro no encontrado" });
        if (dto.MembershipId.HasValue && !await _db.Memberships.AnyAsync(m => m.Id == dto.MembershipId && m.MemberId == dto.MemberId))
            return BadRequest(new { message = "La membresía no corresponde al miembro" });

        var invoice = new GymOS.Domain.Entities.Invoice
        {
            InvoiceNumber = $"INV-{_calendar.Today:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}",
            MemberId = dto.MemberId, MembershipId = dto.MembershipId,
            Subtotal = dto.Subtotal, Discount = dto.Discount, Tax = dto.Tax,
            Total = dto.Subtotal - dto.Discount + dto.Tax,
            DueDate = dto.DueDate, Notes = dto.Notes, Status = PaymentStatus.Pending
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = invoice.Id },
            new InvoiceDto(invoice.Id, invoice.InvoiceNumber, invoice.MemberId, member.FullName,
                invoice.Subtotal, invoice.Discount, invoice.Tax, invoice.Total,
                invoice.Status.ToString(), invoice.DueDate, invoice.PaidAt, invoice.PaymentMethod));
    }

    [HttpGet("summary")]
    public async Task<ActionResult> GetSummary()
    {
        var today = _calendar.Today;
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        var monthStartUtc = _calendar.StartOfDayUtc(thisMonth);
        var nextMonthStartUtc = _calendar.StartOfDayUtc(thisMonth.AddMonths(1));
        return Ok(new
        {
            TotalRevenue = await _db.Invoices.Where(i => i.Status == PaymentStatus.Paid).SumAsync(i => (decimal?)i.Total) ?? 0,
            ThisMonthRevenue = await _db.Invoices.Where(i => i.Status == PaymentStatus.Paid && i.PaidAt >= monthStartUtc && i.PaidAt < nextMonthStartUtc).SumAsync(i => (decimal?)i.Total) ?? 0,
            PendingCount = await _db.Invoices.CountAsync(i => i.Status == PaymentStatus.Pending),
            OverdueCount = await _db.Invoices.CountAsync(i => i.Status == PaymentStatus.Overdue),
        });
    }
}
