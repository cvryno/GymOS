using GymOS.Application.DTOs;
using GymOS.Domain.Entities;
using GymOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClassesController : ControllerBase
{
    private readonly GymDbContext _db;

    public ClassesController(GymDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<GymClassDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null, [FromQuery] string? search = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.GymClasses
            .Include(c => c.Instructor)
            .Include(c => c.Schedules)
            .Include(c => c.Enrollments.Where(e => e.IsActive))
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(c => c.Category == category);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.Contains(search) || c.Description.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new GymClassDto(
                c.Id, c.Name, c.Description, c.Category,
                c.InstructorId, c.Instructor.FullName,
                c.DurationMinutes, c.MaxCapacity,
                c.Color, c.IsActive, c.Enrollments.Count,
                c.Schedules.Select(s => new ScheduleDto(
                    s.Id, s.DayOfWeek.ToString(), s.StartTime.ToString("HH:mm"),
                    s.EndTime.ToString("HH:mm"), s.Room)).ToList()))
            .ToListAsync();

        return Ok(new PagedResult<GymClassDto>(items, total, page, pageSize));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var categories = await _db.GymClasses
            .Select(c => c.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GymClassDto>> GetById(int id)
    {
        var c = await _db.GymClasses
            .Include(c => c.Instructor)
            .Include(c => c.Schedules)
            .Include(c => c.Enrollments.Where(e => e.IsActive))
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c is null) return NotFound();

        return Ok(new GymClassDto(
            c.Id, c.Name, c.Description, c.Category,
            c.InstructorId, c.Instructor.FullName,
            c.DurationMinutes, c.MaxCapacity,
            c.Color, c.IsActive, c.Enrollments.Count,
            c.Schedules.Select(s => new ScheduleDto(
                s.Id, s.DayOfWeek.ToString(), s.StartTime.ToString("HH:mm"),
                s.EndTime.ToString("HH:mm"), s.Room)).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<GymClassDto>> Create([FromBody] CreateGymClassDto dto)
    {
        var instructor = await _db.Instructors.FindAsync(dto.InstructorId);
        if (instructor is null) return BadRequest(new { message = "Instructor no encontrado" });
        if (!instructor.IsActive || dto.DurationMinutes <= 0 || dto.MaxCapacity <= 0)
            return BadRequest(new { message = "Instructor, duración o capacidad no válidos" });
        if (dto.Schedules?.Any(s => s.EndTime <= s.StartTime) == true)
            return BadRequest(new { message = "La hora de fin debe ser posterior a la hora de inicio" });
        if (dto.Schedules?.GroupBy(s => s.DayOfWeek)
            .SelectMany(g => g.SelectMany((schedule, index) => g.Skip(index + 1)
                .Where(other => schedule.StartTime < other.EndTime && other.StartTime < schedule.EndTime)))
            .Any() == true)
            return BadRequest(new { message = "Los horarios de la clase se superponen" });

        var gymClass = new GymClass
        {
            Name = dto.Name, Description = dto.Description,
            Category = dto.Category, InstructorId = dto.InstructorId,
            DurationMinutes = dto.DurationMinutes, MaxCapacity = dto.MaxCapacity,
            Color = dto.Color ?? "#6366f1", IsActive = true
        };

        if (dto.Schedules != null)
            gymClass.Schedules = dto.Schedules.Select(s => new ClassSchedule
            {
                DayOfWeek = s.DayOfWeek, StartTime = s.StartTime,
                EndTime = s.EndTime, Room = s.Room, IsActive = true
            }).ToList();

        _db.GymClasses.Add(gymClass);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = gymClass.Id },
            new GymClassDto(gymClass.Id, gymClass.Name, gymClass.Description, gymClass.Category,
                gymClass.InstructorId, instructor.FullName, gymClass.DurationMinutes,
                gymClass.MaxCapacity, gymClass.Color, gymClass.IsActive, 0,
                gymClass.Schedules.Select(s => new ScheduleDto(s.Id, s.DayOfWeek.ToString(),
                    s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm"), s.Room)).ToList()));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var gymClass = await _db.GymClasses.FindAsync(id);
        if (gymClass is null) return NotFound();
        gymClass.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/enroll")]
    public async Task<IActionResult> Enroll(int id, [FromBody] CheckInDto dto)
    {
        var gymClass = await _db.GymClasses
            .Include(c => c.Enrollments.Where(e => e.IsActive))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (gymClass is null) return NotFound(new { message = "Clase no encontrada" });
        if (!gymClass.IsActive) return BadRequest(new { message = "La clase no está activa" });
        var member = await _db.Members
            .Include(m => m.Memberships.Where(ms => ms.Status == GymOS.Domain.Enums.MembershipStatus.Active))
            .FirstOrDefaultAsync(m => m.Id == dto.MemberId);
        if (member is null) return NotFound(new { message = "Miembro no encontrado" });
        if (member.Status != GymOS.Domain.Enums.MemberStatus.Active ||
            !member.Memberships.Any(m => !m.IsExpired && m.Plan.IncludesClasses))
            return BadRequest(new { message = "El miembro no tiene una membresía vigente que incluya clases" });
        var activeMembership = member.Memberships.First(m => !m.IsExpired && m.Plan.IncludesClasses);
        if (activeMembership.Plan.MaxClassesPerMonth.HasValue)
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var monthlyEnrollments = await _db.ClassEnrollments.CountAsync(e => e.MemberId == member.Id && e.IsActive && e.EnrolledAt >= monthStart);
            if (monthlyEnrollments >= activeMembership.Plan.MaxClassesPerMonth.Value)
                return BadRequest(new { message = "El plan alcanzó el límite mensual de clases" });
        }
        if (gymClass.Enrollments.Any(e => e.MemberId == dto.MemberId))
            return BadRequest(new { message = "El miembro ya está inscrito en esta clase" });

        var isWaitlisted = gymClass.Enrollments.Count >= gymClass.MaxCapacity;
        if (isWaitlisted && await _db.ClassEnrollments.AnyAsync(e => e.ClassId == id && e.MemberId == dto.MemberId && !e.IsActive))
            return BadRequest(new { message = "El miembro ya tiene una inscripción o lista de espera" });

        _db.ClassEnrollments.Add(new ClassEnrollment
        {
            MemberId = dto.MemberId, ClassId = id, IsActive = !isWaitlisted
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = isWaitlisted ? "Clase llena; miembro agregado a lista de espera" : "Inscripción exitosa", waitlisted = isWaitlisted });
    }

    [HttpPost("enrollments/{enrollmentId}/cancel")]
    public async Task<IActionResult> CancelEnrollment(int enrollmentId)
    {
        var enrollment = await _db.ClassEnrollments.Include(e => e.GymClass)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);
        if (enrollment is null) return NotFound();
        if (!enrollment.IsActive) return BadRequest(new { message = "La inscripción no está activa" });

        enrollment.IsActive = false;
        enrollment.UpdatedAt = DateTime.UtcNow;
        var next = await _db.ClassEnrollments
            .Where(e => e.ClassId == enrollment.ClassId && !e.IsActive && e.Id != enrollment.Id)
            .OrderBy(e => e.EnrolledAt).FirstOrDefaultAsync();
        if (next is not null) next.IsActive = true;
        await _db.SaveChangesAsync();
        return Ok(new { promotedMemberId = next?.MemberId });
    }

    [HttpGet("{id}/enrollments")]
    public async Task<IActionResult> GetEnrollments(int id)
    {
        var exists = await _db.GymClasses.AnyAsync(c => c.Id == id);
        if (!exists) return NotFound();
        var entries = await _db.ClassEnrollments.Include(e => e.Member)
            .Where(e => e.ClassId == id)
            .OrderByDescending(e => e.IsActive).ThenBy(e => e.EnrolledAt)
            .Select(e => new { e.Id, e.MemberId, memberName = e.Member.FullName, e.EnrolledAt, e.IsActive })
            .ToListAsync();
        return Ok(entries);
    }
}
