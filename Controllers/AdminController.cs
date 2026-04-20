using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using CitizenAppealsPortal.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CitizenAppealsPortal.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet("deputies/pending")]
    public async Task<IActionResult> GetPendingDeputies()
    {
        var deputies = await _userManager.GetUsersInRoleAsync("Deputy");
        var pending = deputies.Where(d => !d.IsApproved).ToList();
        return Ok(pending);
    }

    [HttpPost("deputies/{id}/approve")]
    public async Task<IActionResult> ApproveDeputy(string id, [FromBody] ApproveDeputyDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (dto.Approve)
        {
            user.IsApproved = true;
            user.AssignedDistrictId = dto.DistrictId;
            await _userManager.UpdateAsync(user);

            if (!await _userManager.IsInRoleAsync(user, "Deputy"))
                await _userManager.AddToRoleAsync(user, "Deputy");
        }
        else
        {
            await _userManager.DeleteAsync(user);
        }

        return NoContent();
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        return Ok(await _context.Categories.ToListAsync());
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, category);
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
    {
        if (id != category.Id) return BadRequest();
        _context.Entry(category).State = EntityState.Modified;
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