using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CitizenAppealsPortal.Controllers;

[Authorize]
[ApiController]
[Route("api/statistics")]
public class StatisticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<StatisticsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Статистика для депутата по его округу.
    /// </summary>
    [HttpGet("deputy")]
    [Authorize(Roles = RoleNames.Deputy)]
    public async Task<IActionResult> GetDeputyStatistics(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);

        if (user?.AssignedDistrictId == null)
        {
            _logger.LogWarning("Депутат {UserId} не привязан к округу", userId);
            return BadRequest("Депутат не привязан к округу.");
        }

        var stats = await CalculateStatisticsAsync(
            a => a.DistrictId == user.AssignedDistrictId, from, to);

        return Ok(new
        {
            stats.Total,
            stats.ByStatus,
            stats.ByCategory
        });
    }

    /// <summary>
    /// Общая статистика для администратора.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> GetAdminStatistics(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var stats = await CalculateStatisticsAsync(null, from, to);

        // Дополнительная группировка по округам
        var byDistrict = await _context.Appeals
            .Where(a => from == null || a.CreatedAt >= from)
            .Where(a => to == null || a.CreatedAt <= to)
            .GroupBy(a => a.DistrictId)
            .Select(g => new
            {
                DistrictId = g.Key,
                Count = g.Count(),
                DistrictName = g.First().District.Name
            })
            .ToListAsync();

        return Ok(new
        {
            stats.Total,
            ByDistrict = byDistrict,
            stats.ByStatus,
            stats.ByCategory
        });
    }

    /// <summary>
    /// Вычисляет общую статистику обращений по заданному фильтру.
    /// </summary>
    private async Task<AppealStats> CalculateStatisticsAsync(
        System.Linq.Expressions.Expression<Func<Appeal, bool>>? additionalFilter,
        DateTime? from,
        DateTime? to)
    {
        var query = _context.Appeals.AsQueryable();

        if (additionalFilter != null)
            query = query.Where(additionalFilter);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        var total = await query.CountAsync();

        var byStatus = await query
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byCategory = await query
            .GroupBy(a => a.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Count = g.Count(),
                CategoryName = g.First().Category.Name
            })
            .ToListAsync();

        return new AppealStats(total, byStatus, byCategory);
    }

    private record AppealStats(
        int Total,
        System.Collections.IEnumerable ByStatus,
        System.Collections.IEnumerable ByCategory);
}