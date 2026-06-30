using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("api/geo")]
public class GeoController : ControllerBase
{
    private readonly HttpClient _http;

    public GeoController(IHttpClientFactory factory)
    {
        _http = factory.CreateClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenAppealsPortal/1.0");
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(q)}&format=json&polygon_geojson=1&limit=1";
        var json = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.GetArrayLength() == 0) return NotFound("Ничего не найдено");
        var geojson = root[0].GetProperty("geojson").ToString();
        return Ok(geojson); // возвращаем чистый GeoJSON объект
    }
}