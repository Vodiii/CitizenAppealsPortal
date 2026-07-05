using System.ComponentModel.DataAnnotations;

namespace CitizenAppealsPortal.Models.DTOs;

public class DistrictDto
{
    // Ответный DTO, не валидируется
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DeputyId { get; set; }
    public string? DeputyFullName { get; set; }
    public string BoundaryGeoJson { get; set; } = string.Empty;
}

public class CreateDistrictDto
{
    [Required(ErrorMessage = "Название округа обязательно")]
    [MaxLength(200, ErrorMessage = "Название округа не должно превышать 200 символов")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "GeoJSON полигона обязателен")]
    public string BoundaryGeoJson { get; set; } = string.Empty;

    public string? DeputyId { get; set; }
}

public class UpdateDistrictDto
{
    [MaxLength(200, ErrorMessage = "Название округа не должно превышать 200 символов")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
    public string? Description { get; set; }

    public string? BoundaryGeoJson { get; set; }
    public string? DeputyId { get; set; }
}

public class ApproveDeputyDto
{
    [Required(ErrorMessage = "Флаг утверждения обязателен")]
    public bool Approve { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Округ должен быть выбран")]
    public int? DistrictId { get; set; }

    [Range(1, 1200, ErrorMessage = "Срок должен быть от 1 до 1200 месяцев")]
    public int? TermMonths { get; set; }
}

public class ExtendTermDto
{
    [Range(1, 1200, ErrorMessage = "Срок должен быть от 1 до 1200 месяцев")]
    public int? TermMonths { get; set; }

    public bool Deactivate { get; set; }
}