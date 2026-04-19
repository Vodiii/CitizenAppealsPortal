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

public class AppealDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string LocationGeoJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public AppealStatus Status { get; set; }
    public string CitizenId { get; set; } = string.Empty;
    public string CitizenFullName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int DistrictId { get; set; }
    public string DistrictName { get; set; } = string.Empty;
    public List<PhotoDto> Photos { get; set; } = new();
    public List<AppealResponseDto> Responses { get; set; } = new();
}

public class PhotoDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

public class AppealResponseDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsSystem { get; set; }
    public string AuthorFullName { get; set; } = string.Empty;
}