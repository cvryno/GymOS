using GymOS.Application.DTOs;
using GymOS.Domain.Entities;
using GymOS.Domain.Enums;
using GymOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembersController : ControllerBase
{
    private readonly GymDbContext _db;

    public MembersController(GymDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<MemberDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Members
            .Include(m => m.Memberships.Where(ms => ms.Status == MembershipStatus.Active))
                .ThenInclude(ms => ms.Plan)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(m =>
                m.FirstName.Contains(search) ||
                m.LastName.Contains(search) ||
                m.Email.Contains(search) ||
                m.PhoneNumber.Contains(search));

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<MemberStatus>(status, true, out var memberStatus))
            query = query.Where(m => m.Status == memberStatus);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => MapToDto(m))
            .ToListAsync();

        return Ok(new PagedResult<MemberDto>(items, total, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MemberDto>> GetById(int id)
    {
        var member = await _db.Members
            .Include(m => m.Memberships).ThenInclude(ms => ms.Plan)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (member is null) return NotFound(new { message = "Miembro no encontrado" });
        return Ok(MapToDto(member));
    }

    [HttpPost]
    public async Task<ActionResult<MemberDto>> Create([FromBody] CreateMemberDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName) ||
            string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.PhoneNumber))
            return BadRequest(new { message = "Nombre, apellido, email y teléfono son obligatorios" });

        dto = dto with { Email = dto.Email.Trim().ToLowerInvariant() };
        if (await _db.Members.AnyAsync(m => m.Email == dto.Email))
            return BadRequest(new { message = "Ya existe un miembro con este email" });

        var member = new Member
        {
            FirstName = dto.FirstName, LastName = dto.LastName,
            Email = dto.Email, PhoneNumber = dto.PhoneNumber,
            DateOfBirth = dto.DateOfBirth, Address = dto.Address,
            EmergencyContactName = dto.EmergencyContactName,
            EmergencyContactPhone = dto.EmergencyContactPhone,
            Notes = dto.Notes, Status = MemberStatus.Active
        };

        _db.Members.Add(member);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = member.Id }, MapToDto(member));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MemberDto>> Update(int id, [FromBody] UpdateMemberDto dto)
    {
        var member = await _db.Members.FindAsync(id);
        if (member is null) return NotFound(new { message = "Miembro no encontrado" });

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Members.AnyAsync(m => m.Id != id && m.Email == normalizedEmail))
            return BadRequest(new { message = "Ya existe un miembro con este email" });

        member.FirstName = dto.FirstName; member.LastName = dto.LastName;
        member.Email = normalizedEmail; member.PhoneNumber = dto.PhoneNumber;
        member.DateOfBirth = dto.DateOfBirth; member.Address = dto.Address;
        member.EmergencyContactName = dto.EmergencyContactName;
        member.EmergencyContactPhone = dto.EmergencyContactPhone;
        member.Notes = dto.Notes; member.UpdatedAt = DateTime.UtcNow;

        if (!Enum.TryParse<MemberStatus>(dto.Status, true, out var status))
            return BadRequest(new { message = "Estado de miembro inválido" });
        member.Status = status;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(member));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var member = await _db.Members.FindAsync(id);
        if (member is null) return NotFound(new { message = "Miembro no encontrado" });

        member.IsDeleted = true;
        member.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/memberships")]
    public async Task<ActionResult<List<MembershipDto>>> GetMemberships(int id)
    {
        var memberships = await _db.Memberships
            .Include(m => m.Plan)
            .Where(m => m.MemberId == id)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MembershipDto(
                m.Id, m.MemberId, m.Member.FullName, m.Member.Email, m.Member.PhoneNumber,
                m.PlanId, m.Plan.Name, m.StartDate, m.EndDate,
                m.Status.ToString(), m.AmountPaid, m.DaysRemaining, m.NotificationSent))
            .ToListAsync();

        return Ok(memberships);
    }

    [HttpGet("{id}/access-history")]
    public async Task<ActionResult<List<AccessLogDto>>> GetAccessHistory(int id)
    {
        var logs = await _db.AccessLogs
            .Include(a => a.Member)
            .Where(a => a.MemberId == id)
            .OrderByDescending(a => a.CheckIn)
            .Take(50)
            .Select(a => new AccessLogDto(
                a.Id, a.MemberId, a.Member.FullName, a.Member.PhotoUrl ?? "",
                a.CheckIn, a.CheckOut,
                a.Duration.HasValue ? $"{(int)a.Duration.Value.TotalHours}h {a.Duration.Value.Minutes}m" : null))
            .ToListAsync();

        return Ok(logs);
    }

    private static MemberDto MapToDto(Member m)
    {
        var active = m.Memberships?.FirstOrDefault(ms => ms.Status == MembershipStatus.Active);
        return new MemberDto(
            m.Id, m.FirstName, m.LastName, m.Email,
            m.PhoneNumber, m.PhotoUrl, m.Status.ToString(),
            m.DateOfBirth, m.Address,
            m.EmergencyContactName, m.EmergencyContactPhone,
            m.CreatedAt,
            active is null ? null : new ActiveMembershipDto(
                active.Id, active.Plan?.Name ?? "", active.StartDate,
                active.EndDate, active.Status.ToString(), active.DaysRemaining));
    }
}
