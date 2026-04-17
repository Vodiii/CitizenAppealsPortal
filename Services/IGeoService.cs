using NetTopologySuite.Geometries;

namespace CitizenAppealsPortal.Services;

public interface IGeoService
{
    Task<int?> FindDistrictIdByPointAsync(Point point);
    Task<bool> IsPointInDistrictAsync(Point point, int districtId);
}