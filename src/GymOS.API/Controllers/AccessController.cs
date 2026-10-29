using GymOS.Application.DTOs;
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
public class AccessController : ControllerBase
{
    private readonly GymDbContext _db;
    private readonly GymCalendar _calendar;

    public AccessController(GymDbContext db, GymCalendar calendar) { _db = db; _calendar = calendar; }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AccessLogDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? date = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.AccessLogs.Include(a => a.Member).AsQueryable();

        if (date.HasValue)
        {
            var localDate = DateOnly.FromDateTime(date.Value);
            var (start, end) = _calendar.DayRangeUtc(localDate);
            query = query.Where(a => a.CheckIn >= start && a.CheckIn < end);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CheckIn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AccessLogDto(
                a.Id, a.MemberId, a.Member.FullName, a.Member.PhotoUrl ?? "",
                a.CheckIn, a.CheckOut,
                a.Duration.HasValue ? $"{(int)a.Duration.Value.TotalHours}h {a.Duration.Value.Minutes}m" : "En curso"))
            .ToListAsync();

        return Ok(new PagedResult<AccessLogDto>(items, total, page, pageSize));
    }

    [HttpGet("today")]
    public async Task<ActionResult<List<AccessLogDto>>> GetToday()
    {
        var (today, tomorrow) = _calendar.DayRangeUtc(_calendar.Today);
        var items = await _db.AccessLogs
            .Include(a => a.Member)
            .Where(a => a.CheckIn >= today && a.CheckIn < tomorrow)
            .OrderByDescending(a => a.CheckIn)
            .Select(a => new AccessLogDto(
                a.Id, a.MemberId, a.Member.FullName, a.Member.PhotoUrl ?? "",
                a.CheckIn, a.CheckOut,
                a.Duration.HasValue ? $"{(int)a.Duration.Value.TotalHours}h {a.Duration.Value.Minutes}m" : "En curso"))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("checkin")]
    public async Task<ActionResult<AccessLogDto>> CheckIn([FromBody] CheckInDto dto)
    {
        var member = await _db.Members
            .Include(m => m.Memberships.Where(ms => ms.Status == MembershipStatus.Active))
            .FirstOrDefaultAsync(m => m.Id == dto.MemberId);

        if (member is null) return NotFound(new { message = "Miembro no encontrado" });
        if (member.Status != MemberStatus.Active)
            return BadRequest(new { message = "El miembro no está activo" });

        var activeMembership = member.Memberships.FirstOrDefault();
        var now = _calendar.UtcNow;
        if (activeMembership is null || activeMembership.StartDate > now || activeMembership.EndDate <= now)
            return BadRequest(new { message = "El miembro no tiene una membresía activa válida" });

        // Check if already checked in
        var existingOpen = await _db.AccessLogs
            .FirstOrDefaultAsync(a => a.MemberId == dto.MemberId && a.CheckOut == null);
        if (existingOpen is not null)
            return BadRequest(new { message = "El miembro ya tiene un check-in abierto" });

        var log = new AccessLog { MemberId = dto.MemberId, CheckIn = now };
        _db.AccessLogs.Add(log);
        await _db.SaveChangesAsync();

        return Ok(new AccessLogDto(log.Id, log.MemberId, member.FullName, member.PhotoUrl ?? "",
            log.CheckIn, log.CheckOut, null));
    }

    [HttpPost("qr-checkin")]
    [Authorize(Policy = "AdminOrReceptionist")]
    public async Task<ActionResult<AccessLogDto>> CheckInByMemberCode([FromBody] QrCheckInDto dto)
    {
        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == dto.MemberId);
        if (member is null) return NotFound(new { message = "Miembro no encontrado" });
        return await CheckIn(new CheckInDto(dto.MemberId));
    }

    [HttpPost("{id}/checkout")]
    public async Task<ActionResult<AccessLogDto>> CheckOut(int id)
    {
        var log = await _db.AccessLogs
            .Include(a => a.Member)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (log is null) return NotFound(new { message = "Registro de acceso no encontrado" });
        if (log.CheckOut.HasValue) return BadRequest(new { message = "El check-out ya fue registrado" });

        log.CheckOut = _calendar.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new AccessLogDto(log.Id, log.MemberId, log.Member.FullName, log.Member.PhotoUrl ?? "",
            log.CheckIn, log.CheckOut,
            $"{(int)log.Duration!.Value.TotalHours}h {log.Duration.Value.Minutes}m"));
    }

    [HttpGet("currently-in")]
    public async Task<ActionResult<List<AccessLogDto>>> GetCurrentlyIn()
    {
        var items = await _db.AccessLogs
            .Include(a => a.Member)
            .Where(a => a.CheckOut == null)
            .OrderByDescending(a => a.CheckIn)
            .Select(a => new AccessLogDto(
                a.Id, a.MemberId, a.Member.FullName, a.Member.PhotoUrl ?? "",
                a.CheckIn, null, "En curso"))
            .ToListAsync();
        return Ok(items);
    }
}

public record QrCheckInDto(int MemberId);
