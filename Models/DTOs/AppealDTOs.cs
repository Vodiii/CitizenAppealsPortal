using System.ComponentModel.DataAnnotations;
using CitizenAppealsPortal.Models;

namespace CitizenAppealsPortal.Models.DTOs;

// === DTO для создания обращения ===
public class CreateAppealDto
{
    [Required(ErrorMessage = "Заголовок обязателен")]
    [MaxLength(200, ErrorMessage = "Заголовок не должен превышать 200 символов")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Описание не должно превышать 2000 символов")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Адрес обязателен")]
    [MaxLength(500, ErrorMessage = "Адрес не должен превышать 500 символов")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Координаты обязательны")]
    public string LocationGeoJson { get; set; } = string.Empty;

    [Required(ErrorMessage = "Категория обязательна")]
    [Range(1, int.MaxValue, ErrorMessage = "Категория должна быть указана")]
    public int CategoryId { get; set; }

    public List<IFormFile>? Photos { get; set; }
}

// === DTO для обновления статуса ===
public class UpdateStatusDto
{
    [Required]
    [EnumDataType(typeof(AppealStatus), ErrorMessage = "Неверный статус обращения")]
    public AppealStatus NewStatus { get; set; }
}

// === DTO для ответа депутата ===
public class AddResponseDto
{
    [Required(ErrorMessage = "Текст ответа обязателен")]
    [MaxLength(2000, ErrorMessage = "Ответ не должен превышать 2000 символов")]
    public string Content { get; set; } = string.Empty;
}

// === DTO для возобновления обращения ===
public class ReopenAppealDto
{
    [Required(ErrorMessage = "Сообщение обязательно")]
    [MaxLength(500, ErrorMessage = "Сообщение не должно превышать 500 символов")]
    public string Message { get; set; } = string.Empty;
}

// === DTO для голосования ===
public class VoteDto
{
    [Required]
    [Range(-1, 1, ErrorMessage = "Голос должен быть 1 или -1")]
    public int VoteType { get; set; }
}

// === DTO‑ответы (формируются сервером, без валидации) ===
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
    public int Score { get; set; }
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
    public List<PhotoDto> Photos { get; set; } = new();
    public List<AppealResponseDto> Responses { get; set; } = new();
    public int? UserVote { get; set; }
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
    public ResponseType ResponseType { get; set; }
    public string AuthorFullName { get; set; } = string.Empty;
}