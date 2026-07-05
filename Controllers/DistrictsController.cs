using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using CitizenAppealsPortal.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Text.Json;

namespace CitizenAppealsPortal.Controllers;

[Authorize(Roles = RoleNames.Admin)]
[ApiController]
[Route("api/[controller]")]
public class DistrictsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly GeoJsonWriter _geoJsonWriter;
    private readonly ILogger<DistrictsController> _logger;

    public DistrictsController(ApplicationDbContext context, ILogger<DistrictsController> logger)
    {
        _context = context;
        _geoJsonWriter = new GeoJsonWriter();
        _logger = logger;
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
        {
            _logger.LogWarning("Невалидный GeoJSON при создании округа: {GeoJson}", dto.BoundaryGeoJson);
            return BadRequest("Некорректный GeoJSON полигона.");
        }

        var district = new District
        {
            Name = dto.Name,
            Description = dto.Description,
            Boundary = polygon,
            DeputyId = dto.DeputyId
        };

        _context.Districts.Add(district);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Округ '{DistrictName}' создан с ID {DistrictId}", district.Name, district.Id);

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
            {
                _logger.LogWarning("Невалидный GeoJSON при обновлении округа {DistrictId}", id);
                return BadRequest("Некорректный GeoJSON полигона.");
            }
            district.Boundary = polygon;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Округ {DistrictId} обновлён", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDistrict(int id)
    {
        var district = await _context.Districts.FindAsync(id);
        if (district == null) return NotFound();

        _context.Districts.Remove(district);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Округ {DistrictId} удалён", id);
        return NoContent();
    }

    /// <summary>
    /// Парсит строку GeoJSON. Поддерживает как Polygon, так и FeatureCollection (берёт первый полигон).
    /// </summary>
    private Polygon? ParsePolygon(string geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(geoJson);
            var root = doc.RootElement;

            // FeatureCollection -> берём первый объект geometry
            if (root.TryGetProperty("type", out var typeElement) &&
                typeElement.GetString() == "FeatureCollection")
            {
                if (root.TryGetProperty("features", out var features) &&
                    features.GetArrayLength() > 0)
                {
                    root = features[0].GetProperty("geometry");
                }
                else return null;
            }
            // Feature -> извлекаем geometry
            else if (typeElement.GetString() == "Feature" &&
                     root.TryGetProperty("geometry", out var geom))
            {
                root = geom;
            }

            // Теперь root должен быть Polygon
            if (root.TryGetProperty("type", out var geomType) &&
                geomType.GetString() == "Polygon")
            {
                var coordinates = root.GetProperty("coordinates");
                var ring = ParseRing(coordinates[0]);
                var factory = new GeometryFactory(new PrecisionModel(), 4326);
                return factory.CreatePolygon(ring);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка парсинга GeoJSON: {GeoJson}", geoJson.Truncate(200));
            return null;
        }
    }

    private LinearRing ParseRing(JsonElement ringArray)
    {
        var points = new List<Coordinate>();
        foreach (var point in ringArray.EnumerateArray())
        {
            double lng = point[0].GetDouble();
            double lat = point[1].GetDouble();
            points.Add(new Coordinate(lng, lat));
        }
        // Замыкаем кольцо, если не замкнуто
        if (points.Count > 0 && points[0] != points[^1])
            points.Add(points[0]);

        var factory = new GeometryFactory(new PrecisionModel(), 4326);
        return factory.CreateLinearRing(points.ToArray());
    }
}

// Вспомогательный extension для усечения строк (чтобы не спамить в логи)
public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}