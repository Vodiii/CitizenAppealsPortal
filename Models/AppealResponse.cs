namespace CitizenAppealsPortal.Models;

public class AppealResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsSystem { get; set; } = false;

    public int AppealId { get; set; }
    public virtual Appeal Appeal { get; set; } = null!;

    public string AuthorId { get; set; } = string.Empty;
    public virtual ApplicationUser Author { get; set; } = null!;
}