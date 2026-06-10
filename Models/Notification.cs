namespace CitizenAppealsPortal.Models;

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;
    public int? AppealId { get; set; }
    public virtual Appeal? Appeal { get; set; }
    public string Type { get; set; } = string.Empty;      // "StatusChange", "NewResponse", "Reopen", "NewVote"
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}