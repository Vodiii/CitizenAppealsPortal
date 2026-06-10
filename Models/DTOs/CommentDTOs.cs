namespace CitizenAppealsPortal.Models.DTOs;

public class CreateCommentDto
{
    public string Text { get; set; } = string.Empty;
}

public class CommentDto
{
    public int Id { get; set; }
    public int AppealId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorFullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}