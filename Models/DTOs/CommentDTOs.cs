using System.ComponentModel.DataAnnotations;

namespace CitizenAppealsPortal.Models.DTOs;

public class CreateCommentDto
{
    [Required(ErrorMessage = "Текст комментария обязателен")]
    [MaxLength(1000, ErrorMessage = "Комментарий не должен превышать 1000 символов")]
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