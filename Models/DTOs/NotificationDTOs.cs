namespace CitizenAppealsPortal.Models.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public int? AppealId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}