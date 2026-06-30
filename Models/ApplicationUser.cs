using Microsoft.AspNetCore.Identity;

namespace CitizenAppealsPortal.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public bool IsApproved { get; set; } = false;
    public DateTime? DateOfBirth { get; set; }   // новое

    public virtual ICollection<Appeal> Appeals { get; set; } = new List<Appeal>();
    public virtual District? AssignedDistrict { get; set; }
    public int? AssignedDistrictId { get; set; }
    public virtual ICollection<DeputyTerm> DeputyTerms { get; set; } = new List<DeputyTerm>();

    // новые коллекции
    public virtual ICollection<UserDocument> Documents { get; set; } = new List<UserDocument>();
    public virtual ICollection<UserSetting> Settings { get; set; } = new List<UserSetting>();
    public virtual ICollection<UserLoginHistory> LoginHistory { get; set; } = new List<UserLoginHistory>();
    public virtual ICollection<UserCategorySubscription> CategorySubscriptions { get; set; } = new List<UserCategorySubscription>();
}