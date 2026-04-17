using CitizenAppealsPortal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CitizenAppealsPortal.Controllers;

[Authorize]
[ApiController]
[Route("api/statistics")]
public class StatisticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StatisticsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("deputy")]
    [Authorize(Roles = "Deputy")]
    public async Task<IActionResult> GetDeputyStatistics(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user?.AssignedDistrictId == null)
            return BadRequest("Депутат не привязан к округу.");

        var query = _context.Appeals
            .Where(a => a.DistrictId == user.AssignedDistrictId);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from);
        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to);

        var total = await query.CountAsync();
        var byStatus = await query.GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byCategory = await query.GroupBy(a => a.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count(), CategoryName = g.First().Category.Name })
            .ToListAsync();

        var avgTime = await query
            .Where(a => a.Responses.Any(r => !r.IsSystem))
            .Select(a => EF.Functions.DateDiffSecond(a.CreatedAt, a.Responses.First().CreatedAt))
            .AverageAsync();

        return Ok(new
        {
            Total = total,
            ByStatus = byStatus,
            ByCategory = byCategory,
            AverageResponseTimeSeconds = avgTime
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminStatistics(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = _context.Appeals.AsQueryable();
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from);
        if (to.HasValue) query = query.Where(a => a.CreatedAt <= to);

        var total = await query.CountAsync();
        var byDistrict = await query.GroupBy(a => a.DistrictId)
            .Select(g => new { DistrictId = g.Key, Count = g.Count(), DistrictName = g.First().District.Name })
            .ToListAsync();

        var byStatus = await query.GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byCategory = await query.GroupBy(a => a.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count(), CategoryName = g.First().Category.Name })
            .ToListAsync();

        return Ok(new
        {
            Total = total,
            ByDistrict = byDistrict,
            ByStatus = byStatus,
            ByCategory = byCategory
        });
    }
}