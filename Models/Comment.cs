namespace CitizenAppealsPortal.Models;

public class Comment
{
    public int Id { get; set; }
    public int AppealId { get; set; }
    public virtual Appeal Appeal { get; set; } = null!;
    public string AuthorId { get; set; } = string.Empty;
    public virtual ApplicationUser Author { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;   // мягкое удаление
}