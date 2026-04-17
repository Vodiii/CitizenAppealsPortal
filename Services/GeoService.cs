using CitizenAppealsPortal.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace CitizenAppealsPortal.Services;

public class GeoService : IGeoService
{
    private readonly ApplicationDbContext _context;

    public GeoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int?> FindDistrictIdByPointAsync(Point point)
    {
        var district = await _context.Districts
            .FirstOrDefaultAsync(d => d.Boundary.Contains(point));

        return district?.Id;
    }

    public async Task<bool> IsPointInDistrictAsync(Point point, int districtId)
    {
        var district = await _context.Districts.FindAsync(districtId);
        if (district == null) return false;
        return district.Boundary.Contains(point);
    }
}