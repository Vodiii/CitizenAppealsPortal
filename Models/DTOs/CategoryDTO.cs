namespace CitizenAppealsPortal.Models.DTOs;

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Code { get; set; }  // новый необязательный параметр
}