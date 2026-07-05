using System.ComponentModel.DataAnnotations;

namespace CitizenAppealsPortal.Models.DTOs;

public class CreateCategoryDto
{
    [Required(ErrorMessage = "Название категории обязательно")]
    [MaxLength(100, ErrorMessage = "Название не должно превышать 100 символов")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(50, ErrorMessage = "Код не должен превышать 50 символов")]
    public string? Code { get; set; }
}