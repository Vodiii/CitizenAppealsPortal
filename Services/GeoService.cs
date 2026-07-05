using CitizenAppealsPortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace CitizenAppealsPortal.Services;

public class GeoService : IGeoService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GeoService> _logger;

    public GeoService(ApplicationDbContext context, ILogger<GeoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Возвращает ID округа, в котором находится заданная точка, или null, если точка не принадлежит ни одному округу.
    /// </summary>
    public async Task<int?> FindDistrictIdByPointAsync(Point point)
    {
        try
        {
            var district = await _context.Districts
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Boundary.Contains(point));

            if (district == null)
            {
                _logger.LogInformation("Точка ({X}, {Y}) не попала ни в один округ", point.X, point.Y);
            }

            return district?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка поиска округа для точки ({X}, {Y})", point.X, point.Y);
            throw;
        }
    }

    /// <summary>
    /// Проверяет, находится ли точка внутри указанного округа.
    /// </summary>
    public async Task<bool> IsPointInDistrictAsync(Point point, int districtId)
    {
        try
        {
            var district = await _context.Districts
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == districtId);

            if (district == null)
            {
                _logger.LogWarning("Округ с ID {DistrictId} не найден", districtId);
                return false;
            }

            return district.Boundary.Contains(point);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка проверки точки ({X}, {Y}) в округе {DistrictId}", point.X, point.Y, districtId);
            throw;
        }
    }
}