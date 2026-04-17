namespace CitizenAppealsPortal.Models;

public class Photo
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int AppealId { get; set; }
    public virtual Appeal Appeal { get; set; } = null!;
}