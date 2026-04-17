using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using CitizenAppealsPortal.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace CitizenAppealsPortal.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class DistrictsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DistrictsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetDistricts()
    {
        var districts = await _context.Districts
            .Include(d => d.Deputy)
            .ToListAsync();
        return Ok(districts);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDistrict(int id)
    {
        var district = await _context.Districts
            .Include(d => d.Deputy)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (district == null) return NotFound();
        return Ok(district);
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
        return CreatedAtAction(nameof(GetDistrict), new { id = district.Id }, district);
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