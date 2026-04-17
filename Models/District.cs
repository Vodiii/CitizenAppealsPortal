using NetTopologySuite.Geometries;

namespace CitizenAppealsPortal.Models;

public class District
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Polygon Boundary { get; set; } = null!;
    public string? DeputyId { get; set; }
    public virtual ApplicationUser? Deputy { get; set; }

    public virtual ICollection<Appeal> Appeals { get; set; } = new List<Appeal>();
}