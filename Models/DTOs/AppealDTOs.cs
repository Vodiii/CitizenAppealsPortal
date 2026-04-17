using CitizenAppealsPortal.Models;

namespace CitizenAppealsPortal.Models.DTOs;

public class CreateAppealDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string LocationGeoJson { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public List<IFormFile>? Photos { get; set; }
}

public class UpdateStatusDto
{
    public AppealStatus NewStatus { get; set; }
}

public class AddResponseDto
{
    public string Content { get; set; } = string.Empty;
}