using Microsoft.AspNetCore.Identity;

namespace CitizenAppealsPortal.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public bool IsApproved { get; set; } = false;

    public virtual ICollection<Appeal> Appeals { get; set; } = new List<Appeal>();
    public virtual District? AssignedDistrict { get; set; }
    public int? AssignedDistrictId { get; set; }
}