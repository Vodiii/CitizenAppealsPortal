using NetTopologySuite.Geometries;

namespace CitizenAppealsPortal.Models;

public class Appeal
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Point Location { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public AppealStatus Status { get; set; } = AppealStatus.New;

    public string CitizenId { get; set; } = string.Empty;
    public virtual ApplicationUser Citizen { get; set; } = null!;

    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;

    public int DistrictId { get; set; }
    public virtual District District { get; set; } = null!;

    public virtual ICollection<AppealResponse> Responses { get; set; } = new List<AppealResponse>();
    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();
}

public enum AppealStatus
{
    New,
    UnderReview,
    InProgress,
    Completed,
    Rejected
}