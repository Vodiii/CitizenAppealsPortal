namespace CitizenAppealsPortal.Models;

public enum ResponseType
{
    Normal,     // обычный ответ депутата
    System,     // системное сообщение (смена статуса)
    Reopen      // возобновление обращения гражданином
}

public class AppealResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsSystem { get; set; } = false;   // оставлено для обратной совместимости, можно заменить на ResponseType
    public ResponseType ResponseType { get; set; } = ResponseType.Normal;
    public int AppealId { get; set; }
    public virtual Appeal Appeal { get; set; } = null!;
    public string AuthorId { get; set; } = string.Empty;
    public virtual ApplicationUser Author { get; set; } = null!;
}