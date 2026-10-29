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
public class EquipmentController : ControllerBase
{
    private readonly GymDbContext _db;
    private readonly GymCalendar _calendar;

    public EquipmentController(GymDbContext db, GymCalendar calendar) { _db = db; _calendar = calendar; }

    [HttpGet]
    public async Task<ActionResult<PagedResult<EquipmentDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? category = null,
        [FromQuery] string? search = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Equipment.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<EquipmentStatus>(status, true, out var es))
            query = query.Where(e => e.Status == es);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(e => e.Category == category);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(e => e.Name.Contains(search) || e.Brand.Contains(search) || e.Model.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new EquipmentDto(
                e.Id, e.Name, e.Brand, e.Model, e.SerialNumber,
                e.Status.ToString(), e.Category, e.PurchaseDate, e.PurchasePrice,
                e.LastMaintenanceDate, e.NextMaintenanceDate, e.Location, e.Notes))
            .ToListAsync();

        return Ok(new PagedResult<EquipmentDto>(items, total, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EquipmentDto>> GetById(int id)
    {
        var e = await _db.Equipment.FindAsync(id);
        if (e is null) return NotFound();
        return Ok(new EquipmentDto(e.Id, e.Name, e.Brand, e.Model, e.SerialNumber,
            e.Status.ToString(), e.Category, e.PurchaseDate, e.PurchasePrice,
            e.LastMaintenanceDate, e.NextMaintenanceDate, e.Location, e.Notes));
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentDto>> Create([FromBody] CreateEquipmentDto dto)
    {
        var equipment = new Equipment
        {
            Name = dto.Name, Brand = dto.Brand, Model = dto.Model,
            SerialNumber = dto.SerialNumber, Category = dto.Category,
            PurchaseDate = dto.PurchaseDate, PurchasePrice = dto.PurchasePrice,
            Location = dto.Location, Notes = dto.Notes,
            Status = EquipmentStatus.Operational
        };
        _db.Equipment.Add(equipment);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = equipment.Id },
            new EquipmentDto(equipment.Id, equipment.Name, equipment.Brand, equipment.Model,
                equipment.SerialNumber, equipment.Status.ToString(), equipment.Category,
                equipment.PurchaseDate, equipment.PurchasePrice, null, null, equipment.Location, equipment.Notes));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] EquipmentDto dto)
    {
        var equipment = await _db.Equipment.FindAsync(id);
        if (equipment is null) return NotFound();

        equipment.Name = dto.Name; equipment.Brand = dto.Brand;
        equipment.Model = dto.Model; equipment.Location = dto.Location;
        equipment.Notes = dto.Notes; equipment.LastMaintenanceDate = dto.LastMaintenanceDate;
        equipment.NextMaintenanceDate = dto.NextMaintenanceDate;
        if (Enum.TryParse<EquipmentStatus>(dto.Status, true, out var es)) equipment.Status = es;
        equipment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var equipment = await _db.Equipment.FindAsync(id);
        if (equipment is null) return NotFound();
        equipment.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var cats = await _db.Equipment.Select(e => e.Category).Distinct().OrderBy(c => c).ToListAsync();
        return Ok(cats);
    }

    [HttpGet("maintenance-due")]
    public async Task<ActionResult<List<EquipmentDto>>> GetMaintenanceDue()
    {
        var items = await _db.Equipment
            .Where(e => e.NextMaintenanceDate <= _calendar.StartOfDayUtc(_calendar.Today.AddDays(8)))
            .OrderBy(e => e.NextMaintenanceDate)
            .Select(e => new EquipmentDto(e.Id, e.Name, e.Brand, e.Model, e.SerialNumber,
                e.Status.ToString(), e.Category, e.PurchaseDate, e.PurchasePrice,
                e.LastMaintenanceDate, e.NextMaintenanceDate, e.Location, e.Notes))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("{id}/maintenance")]
    [Authorize(Policy = "AdminOrReceptionist")]
    public async Task<IActionResult> RecordMaintenance(int id, [FromBody] MaintenanceRecordDto dto)
    {
        if (dto.MaintenanceDate == default ||
            (dto.NextMaintenanceDate.HasValue && DateOnly.FromDateTime(dto.NextMaintenanceDate.Value) <= DateOnly.FromDateTime(dto.MaintenanceDate)))
            return BadRequest(new { message = "La siguiente fecha debe ser posterior al mantenimiento" });
        var equipment = await _db.Equipment.FindAsync(id);
        if (equipment is null) return NotFound();
        equipment.LastMaintenanceDate = dto.MaintenanceDate;
        equipment.NextMaintenanceDate = dto.NextMaintenanceDate;
        equipment.Status = EquipmentStatus.Operational;
        equipment.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? equipment.Notes : dto.Notes;
        equipment.UpdatedAt = _calendar.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record MaintenanceRecordDto(DateTime MaintenanceDate, DateTime? NextMaintenanceDate, string? Notes);
