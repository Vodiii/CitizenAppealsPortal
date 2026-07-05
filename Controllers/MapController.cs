using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using CitizenAppealsPortal.Models.DTOs;
using CitizenAppealsPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Text.Json;

namespace CitizenAppealsPortal.Controllers;

[ApiController]
[Route("api/map")]
public class MapController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IGeoService _geoService;
    private readonly GeoJsonWriter _geoJsonWriter;
    private readonly ILogger<MapController> _logger;

    public MapController(ApplicationDbContext context,
                         IGeoService geoService,
                         ILogger<MapController> logger)
    {
        _context = context;
        _geoService = geoService;
        _geoJsonWriter = new GeoJsonWriter();
        _logger = logger;
    }

    /// <summary>
    /// Возвращает границы всех округов в формате GeoJSON FeatureCollection.
    /// </summary>
    [HttpGet("districts/geojson")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDistrictsGeoJson()
    {
        var districts = await _context.Districts.ToListAsync();
        var features = new List<object>();

        foreach (var d in districts)
        {
            try
            {
                var geoJson = _geoJsonWriter.Write(d.Boundary);
                var geometryObject = JsonSerializer.Deserialize<object>(geoJson);
                features.Add(new
                {
                    type = "Feature",
                    geometry = geometryObject,
                    properties = new { d.Id, d.Name, d.Description }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сериализации границы округа {DistrictId}", d.Id);
            }
        }

        return Ok(new
        {
            type = "FeatureCollection",
            features
        });
    }

    /// <summary>
    /// Возвращает точки обращений в формате GeoJSON FeatureCollection с возможностью фильтрации.
    /// </summary>
    [HttpGet("appeals/geojson")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAppealsGeoJson(
        [FromQuery] int? categoryId,
        [FromQuery] AppealStatus? status,
        [FromQuery] int? districtId)
    {
        var query = _context.Appeals
            .Include(a => a.Category)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId);
        if (status.HasValue)
            query = query.Where(a => a.Status == status);
        if (districtId.HasValue)
            query = query.Where(a => a.DistrictId == districtId);

        var appeals = await query.ToListAsync();
        var features = new List<object>();

        foreach (var a in appeals)
        {
            try
            {
                var geoJson = _geoJsonWriter.Write(a.Location);
                var geometryObject = JsonSerializer.Deserialize<object>(geoJson);
                features.Add(new
                {
                    type = "Feature",
                    geometry = geometryObject,
                    properties = new
                    {
                        a.Id,
                        a.Title,
                        a.Status,
                        Category = a.Category.Name,
                        a.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сериализации точки обращения {AppealId}", a.Id);
            }
        }

        return Ok(new
        {
            type = "FeatureCollection",
            features
        });
    }

    /// <summary>
    /// Определяет, в каком округе находится точка с заданными координатами.
    /// </summary>
    [HttpPost("find-district")]
    [AllowAnonymous]
    public async Task<IActionResult> FindDistrict([FromBody] PointGeoJsonDto pointDto)
    {
        var point = ParsePoint(pointDto.GeoJson);
        if (point == null)
        {
            _logger.LogWarning("Невалидный GeoJSON точки при определении округа: {GeoJson}", pointDto.GeoJson);
            return BadRequest("Некорректный GeoJSON точки.");
        }

        var districtId = await _geoService.FindDistrictIdByPointAsync(point);
        if (districtId == null)
        {
            _logger.LogInformation("Округ не найден для координат {Coords}", pointDto.GeoJson);
            return NotFound("Округ не найден.");
        }

        var district = await _context.Districts.FindAsync(districtId);
        _logger.LogInformation("Определён округ {DistrictId} для координат", districtId);
        return Ok(new { DistrictId = districtId, DistrictName = district?.Name });
    }

    private Point? ParsePoint(string geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(geoJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var type) && type.GetString() == "Point" &&
                root.TryGetProperty("coordinates", out var coords))
            {
                var lng = coords[0].GetDouble();
                var lat = coords[1].GetDouble();
                return new Point(lng, lat) { SRID = 4326 };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка парсинга GeoJSON точки: {GeoJson}", geoJson.Truncate(200));
            return null;
        }
    }
}