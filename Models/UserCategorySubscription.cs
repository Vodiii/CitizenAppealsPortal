namespace CitizenAppealsPortal.Models;

public class UserCategorySubscription
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
}