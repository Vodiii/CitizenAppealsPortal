using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace CitizenAppealsPortal.Controllers;

// DTO классы внутри файла (можно вынести в отдельные файлы)
public class DistrictDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DeputyId { get; set; }
    public string? DeputyFullName { get; set; }
    public string BoundaryGeoJson { get; set; } = string.Empty;
}

public class CreateDistrictDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BoundaryGeoJson { get; set; } = string.Empty;
    public string? DeputyId { get; set; }
}

public class UpdateDistrictDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BoundaryGeoJson { get; set; }
    public string? DeputyId { get; set; }
}

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class DistrictsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly GeoJsonWriter _geoJsonWriter;

    public DistrictsController(ApplicationDbContext context)
    {
        _context = context;
        _geoJsonWriter = new GeoJsonWriter();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetDistricts()
    {
        var districts = await _context.Districts
            .Include(d => d.Deputy)
            .ToListAsync();

        var dtos = districts.Select(d => new DistrictDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            DeputyId = d.DeputyId,
            DeputyFullName = d.Deputy?.FullName,
            BoundaryGeoJson = _geoJsonWriter.Write(d.Boundary)
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDistrict(int id)
    {
        var district = await _context.Districts
            .Include(d => d.Deputy)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (district == null) return NotFound();

        var dto = new DistrictDto
        {
            Id = district.Id,
            Name = district.Name,
            Description = district.Description,
            DeputyId = district.DeputyId,
            DeputyFullName = district.Deputy?.FullName,
            BoundaryGeoJson = _geoJsonWriter.Write(district.Boundary)
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDistrict([FromBody] CreateDistrictDto dto)
    {
        var polygon = ParsePolygon(dto.BoundaryGeoJson);
        if (polygon == null)
            return BadRequest("Некорректный GeoJSON полигона.");

        var district = new District
        {
            Name = dto.Name,
            Description = dto.Description,
            Boundary = polygon,
            DeputyId = dto.DeputyId
        };
        _context.Districts.Add(district);
        await _context.SaveChangesAsync();

        var createdDistrict = await _context.Districts
            .Include(d => d.Deputy)
            .FirstOrDefaultAsync(d => d.Id == district.Id);

        var dtoResponse = new DistrictDto
        {
            Id = createdDistrict!.Id,
            Name = createdDistrict.Name,
            Description = createdDistrict.Description,
            DeputyId = createdDistrict.DeputyId,
            DeputyFullName = createdDistrict.Deputy?.FullName,
            BoundaryGeoJson = _geoJsonWriter.Write(createdDistrict.Boundary)
        };

        return CreatedAtAction(nameof(GetDistrict), new { id = district.Id }, dtoResponse);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDistrict(int id, [FromBody] UpdateDistrictDto dto)
    {
        var district = await _context.Districts.FindAsync(id);
        if (district == null) return NotFound();

        district.Name = dto.Name;
        district.Description = dto.Description;
        district.DeputyId = dto.DeputyId;

        if (!string.IsNullOrEmpty(dto.BoundaryGeoJson))
        {
            var polygon = ParsePolygon(dto.BoundaryGeoJson);
            if (polygon == null)
                return BadRequest("Некорректный GeoJSON полигона.");
            district.Boundary = polygon;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDistrict(int id)
    {
        var district = await _context.Districts.FindAsync(id);
        if (district == null) return NotFound();
        _context.Districts.Remove(district);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private Polygon? ParsePolygon(string geoJson)
    {
        try
        {
            var reader = new GeoJsonReader();
            var geom = reader.Read<Geometry>(geoJson);
            return geom as Polygon;
        }
        catch
        {
            return null;
        }
    }
}