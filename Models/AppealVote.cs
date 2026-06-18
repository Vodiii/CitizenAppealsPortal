namespace CitizenAppealsPortal.Models;

public class AppealVote
{
    public int Id { get; set; }
    public int AppealId { get; set; }
    public virtual Appeal Appeal { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;
    public int VoteType { get; set; }   // 1 = Up, -1 = Down
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}