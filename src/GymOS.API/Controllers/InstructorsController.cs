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
public class InstructorsController : ControllerBase
{
    private readonly GymDbContext _db;

    public InstructorsController(GymDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<InstructorDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Instructors.Include(i => i.Classes).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(i =>
                i.FirstName.Contains(search) ||
                i.LastName.Contains(search) ||
                i.Email.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(i => i.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new InstructorDto(
                i.Id, i.FirstName, i.LastName, i.Email, i.PhoneNumber,
                i.PhotoUrl, i.Specialties, i.Bio, i.IsActive, i.HireDate,
                i.Classes.Count))
            .ToListAsync();

        return Ok(new PagedResult<InstructorDto>(items, total, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InstructorDto>> GetById(int id)
    {
        var i = await _db.Instructors.Include(i => i.Classes).FirstOrDefaultAsync(x => x.Id == id);
        if (i is null) return NotFound();
        return Ok(new InstructorDto(i.Id, i.FirstName, i.LastName, i.Email, i.PhoneNumber,
            i.PhotoUrl, i.Specialties, i.Bio, i.IsActive, i.HireDate, i.Classes.Count));
    }

    [HttpPost]
    public async Task<ActionResult<InstructorDto>> Create([FromBody] CreateInstructorDto dto)
    {
        var instructor = new Instructor
        {
            FirstName = dto.FirstName, LastName = dto.LastName,
            Email = dto.Email, PhoneNumber = dto.PhoneNumber,
            Specialties = dto.Specialties, Bio = dto.Bio,
            Certifications = dto.Certifications, HireDate = dto.HireDate, IsActive = true
        };
        _db.Instructors.Add(instructor);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = instructor.Id },
            new InstructorDto(instructor.Id, instructor.FirstName, instructor.LastName,
                instructor.Email, instructor.PhoneNumber, instructor.PhotoUrl,
                instructor.Specialties, instructor.Bio, instructor.IsActive, instructor.HireDate, 0));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateInstructorDto dto)
    {
        var instructor = await _db.Instructors.FindAsync(id);
        if (instructor is null) return NotFound();
        instructor.FirstName = dto.FirstName; instructor.LastName = dto.LastName;
        instructor.Email = dto.Email; instructor.PhoneNumber = dto.PhoneNumber;
        instructor.Specialties = dto.Specialties; instructor.Bio = dto.Bio;
        instructor.Certifications = dto.Certifications; instructor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var instructor = await _db.Instructors.FindAsync(id);
        if (instructor is null) return NotFound();
        instructor.IsDeleted = true; instructor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
