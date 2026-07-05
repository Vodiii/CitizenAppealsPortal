using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using CitizenAppealsPortal.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CitizenAppealsPortal.Controllers;

[Authorize]
[ApiController]
public class CommentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<CommentsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Возвращает список неудалённых комментариев для заданного обращения.
    /// </summary>
    [HttpGet("/api/appeals/{appealId}/comments")]
    public async Task<IActionResult> GetComments(int appealId)
    {
        var comments = await _context.Comments
            .Include(c => c.Author)
            .Where(c => c.AppealId == appealId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                AppealId = c.AppealId,
                Text = c.Text,
                AuthorId = c.AuthorId,
                AuthorFullName = c.Author.FullName,
                CreatedAt = c.CreatedAt,
                IsDeleted = c.IsDeleted
            })
            .ToListAsync();

        return Ok(comments);
    }

    /// <summary>
    /// Создаёт новый комментарий к обращению.
    /// </summary>
    [HttpPost("/api/appeals/{appealId}/comments")]
    public async Task<IActionResult> CreateComment(int appealId, [FromBody] CreateCommentDto dto)
    {
        var appeal = await _context.Appeals.FindAsync(appealId);
        if (appeal == null)
        {
            _logger.LogWarning("Попытка добавить комментарий к несуществующему обращению {AppealId}", appealId);
            return NotFound("Обращение не найдено.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var comment = new Comment
        {
            AppealId = appealId,
            AuthorId = userId!,
            Text = dto.Text
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        var created = await _context.Comments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == comment.Id);

        _logger.LogInformation("Пользователь {UserId} добавил комментарий {CommentId} к обращению {AppealId}",
            userId, comment.Id, appealId);

        return CreatedAtAction(nameof(GetComments), new { appealId }, new CommentDto
        {
            Id = created!.Id,
            AppealId = created.AppealId,
            Text = created.Text,
            AuthorId = created.AuthorId,
            AuthorFullName = created.Author.FullName,
            CreatedAt = created.CreatedAt,
            IsDeleted = created.IsDeleted
        });
    }

    /// <summary>
    /// Редактирует существующий комментарий (доступно только автору).
    /// </summary>
    [HttpPut("/api/comments/{id}")]
    public async Task<IActionResult> UpdateComment(int id, [FromBody] CreateCommentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null) return NotFound();

        if (comment.AuthorId != userId)
        {
            _logger.LogWarning("Пользователь {UserId} попытался отредактировать чужой комментарий {CommentId}", userId, id);
            return StatusCode(403, new { message = "Редактировать можно только свои комментарии." });
        }

        comment.Text = dto.Text;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Комментарий {CommentId} отредактирован пользователем {UserId}", id, userId);
        return NoContent();
    }

    /// <summary>
    /// Мягко удаляет комментарий (доступно автору или администратору).
    /// </summary>
    [HttpDelete("/api/comments/{id}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null) return NotFound();

        bool isAdmin = User.IsInRole(RoleNames.Admin);
        if (comment.AuthorId != userId && !isAdmin)
        {
            _logger.LogWarning("Пользователь {UserId} попытался удалить чужой комментарий {CommentId}", userId, id);
            return StatusCode(403, new { message = "Удалять можно только свои комментарии. Администратор может удалять любые." });
        }

        comment.IsDeleted = true;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Комментарий {CommentId} удалён пользователем {UserId}", id, userId);
        return NoContent();
    }
}