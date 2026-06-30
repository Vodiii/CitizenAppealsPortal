namespace CitizenAppealsPortal.Models.DTOs;

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

public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class UserDocumentDto
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class CreateDocumentDto
{
    public string DocumentType { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
}

public class UserSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class UpdateSettingsDto
{
    public List<UserSettingDto> Settings { get; set; } = new();
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

public class UpdateSubscriptionsDto
{
    public List<int> CategoryIds { get; set; } = new();  // список ID категорий, на которые подписываемся (заменяет текущие)
}