namespace CitizenAppealsPortal.Models;

public class DeputyTerm
{
    public int Id { get; set; }
    public string DeputyId { get; set; } = string.Empty;       // FK на AspNetUsers
    public virtual ApplicationUser Deputy { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;                  // активен ли срок (может быть деактивирован админом досрочно)
}