using System.ComponentModel.DataAnnotations;

namespace CitizenAppealsPortal.Models.DTOs;

public class PointGeoJsonDto
{
    [Required(ErrorMessage = "GeoJSON точки обязателен")]
    [MinLength(1, ErrorMessage = "GeoJSON не может быть пустым")]
    public string GeoJson { get; set; } = string.Empty;
}