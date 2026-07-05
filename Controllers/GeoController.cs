using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CitizenAppealsPortal.Controllers;

[ApiController]
[Route("api/geo")]
public class GeoController : ControllerBase
{
    private const string NominatimBaseUrl = "https://nominatim.openstreetmap.org";
    private const string NominatimUserAgent = "CitizenAppealsPortal/1.0";

    private readonly HttpClient _http;
    private readonly ILogger<GeoController> _logger;

    public GeoController(IHttpClientFactory factory, ILogger<GeoController> logger)
    {
        _http = factory.CreateClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(NominatimUserAgent);
        _logger = logger;
    }

    /// <summary>
    /// Поиск географического объекта через Nominatim и возврат его полигона в формате GeoJSON.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Введите поисковый запрос.");

        var url = $"{NominatimBaseUrl}/search?q={Uri.EscapeDataString(q)}&format=json&polygon_geojson=1&limit=1";

        try
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nominatim вернул статус {StatusCode} для запроса '{Query}'",
                    (int)response.StatusCode, q);
                return StatusCode(502, "Сервис геокодирования временно недоступен.");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetArrayLength() == 0)
            {
                _logger.LogInformation("Поиск Nominatim не дал результатов для '{Query}'", q);
                return NotFound("Ничего не найдено.");
            }

            var geojson = root[0].GetProperty("geojson").ToString();
            _logger.LogInformation("Успешный поиск Nominatim для '{Query}'", q);
            return Ok(geojson);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ошибка запроса к Nominatim для '{Query}'", q);
            return StatusCode(502, "Ошибка связи с сервисом геокодирования.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ошибка парсинга ответа Nominatim для '{Query}'", q);
            return StatusCode(500, "Некорректный ответ от сервиса геокодирования.");
        }
    }
}