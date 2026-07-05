using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using CitizenAppealsPortal.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CitizenAppealsPortal.Controllers;

[Authorize(Roles = RoleNames.Admin)]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    // ========== Депутаты ==========

    [HttpGet("deputies/pending")]
    public async Task<IActionResult> GetPendingDeputies()
    {
        var deputies = await _userManager.GetUsersInRoleAsync(RoleNames.Deputy);
        var pending = deputies.Where(d => !d.IsApproved).ToList();
        return Ok(pending);
    }

    [HttpPost("deputies/{id}/approve")]
    public async Task<IActionResult> ApproveDeputy(string id, [FromBody] ApproveDeputyDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (!dto.Approve)
        {
            // Отклонение заявки – удаляем пользователя (поведение оставлено как было)
            await _userManager.DeleteAsync(user);
            return NoContent();
        }

        user.IsApproved = true;
        user.AssignedDistrictId = dto.DistrictId;
        await _userManager.UpdateAsync(user);

        if (!await _userManager.IsInRoleAsync(user, RoleNames.Deputy))
            await _userManager.AddToRoleAsync(user, RoleNames.Deputy);

        var term = new DeputyTerm
        {
            DeputyId = id,
            StartDate = DateTime.UtcNow,
            // Если срок не указан, назначаем бессрочный срок (до максимальной даты)
            EndDate = dto.TermMonths.HasValue
                ? DateTime.UtcNow.AddMonths(dto.TermMonths.Value)
                : DateTime.MaxValue,
            IsActive = true
        };
        _context.DeputyTerms.Add(term);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Депутат {DeputyId} утверждён администратором", id);
        return NoContent();
    }

    [HttpPost("deputies/{id}/extend-term")]
    public async Task<IActionResult> ExtendTerm(string id, [FromBody] ExtendTermDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var activeTerm = await _context.DeputyTerms
            .FirstOrDefaultAsync(t => t.DeputyId == id && t.IsActive);

        if (activeTerm == null)
            return BadRequest("Нет активного срока для этого депутата.");

        if (dto.Deactivate)
        {
            activeTerm.IsActive = false;
            activeTerm.EndDate = DateTime.UtcNow;
        }
        else if (dto.TermMonths.HasValue)
        {
            activeTerm.EndDate = activeTerm.EndDate.AddMonths(dto.TermMonths.Value);
        }
        else
        {
            return BadRequest("Укажите TermMonths или Deactivate = true");
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Срок депутата {DeputyId} изменён", id);
        return NoContent();
    }

    [HttpGet("deputies/{id}/terms")]
    public async Task<IActionResult> GetDeputyTerms(string id)
    {
        var terms = await _context.DeputyTerms
            .Where(t => t.DeputyId == id)
            .OrderByDescending(t => t.StartDate)
            .ToListAsync();
        return Ok(terms);
    }

    // ========== Категории ==========

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        // Возвращаем все категории (админ видит все, граждане/депутаты тоже видят все)
        return Ok(await _context.Categories.ToListAsync());
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            Code = dto.Code
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, category);
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CreateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.IsActive = dto.IsActive;
        if (!string.IsNullOrEmpty(dto.Code))   // явная проверка, чтобы не затирать null-ом
            category.Code = dto.Code;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

