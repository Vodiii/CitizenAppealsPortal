namespace CitizenAppealsPortal.Models.DTOs;

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

public class ApproveDeputyDto 
{
    public bool Approve { get; set; }
    public int? DistrictId { get; set; }
}

public class DistrictDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DeputyId { get; set; }
    public string? DeputyFullName { get; set; }
    public string BoundaryGeoJson { get; set; } = string.Empty;
}