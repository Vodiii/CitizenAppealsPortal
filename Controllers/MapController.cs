using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using CitizenAppealsPortal.Models.DTOs;
using CitizenAppealsPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public MapController(ApplicationDbContext context, IGeoService geoService)
    {
        _context = context;
        _geoService = geoService;
        _geoJsonWriter = new GeoJsonWriter();
    }

    [HttpGet("districts/geojson")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDistrictsGeoJson()
    {
        var districts = await _context.Districts.ToListAsync();
        var features = new List<object>();

        foreach (var d in districts)
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

        return Ok(new
        {
            type = "FeatureCollection",
            features = features
        });
    }

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

        return Ok(new
        {
            type = "FeatureCollection",
            features = features
        });
    }

    [HttpPost("find-district")]
    [AllowAnonymous]
    public async Task<IActionResult> FindDistrict([FromBody] PointGeoJsonDto pointDto)
    {
        var point = ParsePoint(pointDto.GeoJson);
        if (point == null)
            return BadRequest("Некорректный GeoJSON точки.");

        var districtId = await _geoService.FindDistrictIdByPointAsync(point);
        if (districtId == null)
            return NotFound("Округ не найден.");

        var district = await _context.Districts.FindAsync(districtId);
        return Ok(new { DistrictId = districtId, DistrictName = district?.Name });
    }

    private Point? ParsePoint(string geoJson)
    {
        try
        {
            var reader = new GeoJsonReader();
            var geom = reader.Read<Geometry>(geoJson);
            return geom as Point;
        }
        catch
        {
            return null;
        }
    }
}