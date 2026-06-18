using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using CitizenAppealsPortal.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CitizenAppealsPortal.Controllers;

[Authorize]
[ApiController]
public class CommentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CommentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET /api/appeals/{appealId}/comments
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

    // POST /api/appeals/{appealId}/comments
    [HttpPost("/api/appeals/{appealId}/comments")]
    public async Task<IActionResult> CreateComment(int appealId, [FromBody] CreateCommentDto dto)
    {
        var appeal = await _context.Appeals.FindAsync(appealId);
        if (appeal == null) return NotFound("Обращение не найдено.");

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

    // PUT /api/comments/{id}
    [HttpPut("/api/comments/{id}")]
    public async Task<IActionResult> UpdateComment(int id, [FromBody] CreateCommentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null) return NotFound();

        if (comment.AuthorId != userId)
            return StatusCode(403, new { message = "Редактировать можно только свои комментарии." });

        comment.Text = dto.Text;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/comments/{id}
    [HttpDelete("/api/comments/{id}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (comment.AuthorId != userId && !isAdmin)
            return StatusCode(403, new { message = "Удалять можно только свои комментарии." });

        comment.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}