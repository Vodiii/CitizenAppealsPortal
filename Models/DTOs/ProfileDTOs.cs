using System.ComponentModel.DataAnnotations;

namespace CitizenAppealsPortal.Models.DTOs;

// === Ответные DTO (формируются сервером, без валидации) ===
public class ProfileDto
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DeputyInfoDto? Deputy { get; set; }
}

public class DeputyInfoDto
{
    public int? DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public string? DeputyFullName { get; set; }
    public string? DeputyEmail { get; set; }
    public string? DeputyPhone { get; set; }
    public bool IsActiveTerm { get; set; }
}

public class UserDocumentDto
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class LoginHistoryDto
{
    public DateTime LoginTime { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class CategorySubscriptionDto
{
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool Subscribed { get; set; }
}

// === DTO, принимаемые от клиента (с валидацией) ===
public class UpdateProfileDto
{
    [Required(ErrorMessage = "ФИО обязательно")]
    [MaxLength(100, ErrorMessage = "ФИО не должно превышать 100 символов")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Телефон не должен превышать 20 символов")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date, ErrorMessage = "Некорректная дата рождения")]
    public DateTime? DateOfBirth { get; set; }
}

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Текущий пароль обязателен")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Новый пароль обязателен")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать не менее 6 символов")]
    public string NewPassword { get; set; } = string.Empty;
}

public class CreateDocumentDto
{
    [Required(ErrorMessage = "Тип документа обязателен")]
    [MaxLength(50, ErrorMessage = "Тип документа не должен превышать 50 символов")]
    public string DocumentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Файл обязателен")]
    public IFormFile File { get; set; } = null!;
}

public class UserSettingDto
{
    [Required(ErrorMessage = "Ключ настройки обязателен")]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "Значение настройки обязательно")]
    [MaxLength(500)]
    public string Value { get; set; } = string.Empty;
}

public class UpdateSettingsDto
{
    [Required(ErrorMessage = "Список настроек обязателен")]
    [MinLength(1, ErrorMessage = "Необходимо передать хотя бы одну настройку")]
    public List<UserSettingDto> Settings { get; set; } = new();
}

public class UpdateSubscriptionsDto
{
    [Required(ErrorMessage = "Список категорий обязателен")]
    public List<int> CategoryIds { get; set; } = new();
}