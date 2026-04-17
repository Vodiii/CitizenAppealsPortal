using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using CitizenAppealsPortal.Models.DTOs;
using CitizenAppealsPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Security.Claims;

namespace CitizenAppealsPortal.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AppealsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGeoService _geoService;
    private readonly IFileService _fileService;

    public AppealsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        IGeoService geoService, IFileService fileService)
    {
        _context = context;
        _userManager = userManager;
        _geoService = geoService;
        _fileService = fileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAppeals(
        [FromQuery] int? categoryId,
        [FromQuery] AppealStatus? status,
        [FromQuery] int? districtId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Appeals
            .Include(a => a.Category)
            .Include(a => a.District)
            .Include(a => a.Citizen)
            .Include(a => a.Photos)
            .AsQueryable();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        var roles = await _userManager.GetRolesAsync(user!);

        if (roles.Contains("Citizen"))
        {
            query = query.Where(a => a.CitizenId == userId);
        }
        else if (roles.Contains("Deputy"))
        {
            if (user!.AssignedDistrictId == null)
                return BadRequest("Депутат не привязан к округу.");
            query = query.Where(a => a.DistrictId == user.AssignedDistrictId);
        }

        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId);
        if (status.HasValue)
            query = query.Where(a => a.Status == status);
        if (districtId.HasValue)
            query = query.Where(a => a.DistrictId == districtId);
        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate);
        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { Total = total, Page = page, PageSize = pageSize, Items = items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppeal(int id)
    {
        var appeal = await _context.Appeals
            .Include(a => a.Category)
            .Include(a => a.District)
            .Include(a => a.Citizen)
            .Include(a => a.Photos)
            .Include(a => a.Responses).ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appeal == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        var roles = await _userManager.GetRolesAsync(user!);

        if (roles.Contains("Citizen") && appeal.CitizenId != userId)
            return Forbid();
        if (roles.Contains("Deputy") && appeal.DistrictId != user!.AssignedDistrictId)
            return Forbid();

        return Ok(appeal);
    }

    [HttpPost]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> CreateAppeal([FromForm] CreateAppealDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);

        var point = ParsePoint(dto.LocationGeoJson);
        if (point == null)
            return BadRequest("Некорректные координаты.");

        var districtId = await _geoService.FindDistrictIdByPointAsync(point);
        if (districtId == null)
            return BadRequest("Не удалось определить округ для указанного местоположения.");

        var category = await _context.Categories.FindAsync(dto.CategoryId);
        if (category == null)
            return BadRequest("Категория не найдена.");

        var appeal = new Appeal
        {
            Title = dto.Title,
            Description = dto.Description,
            Address = dto.Address,
            Location = point,
            CitizenId = userId!,
            CategoryId = dto.CategoryId,
            DistrictId = districtId.Value,
            Status = AppealStatus.New
        };

        _context.Appeals.Add(appeal);
        await _context.SaveChangesAsync();

        if (dto.Photos != null)
        {
            foreach (var file in dto.Photos)
            {
                if (file.Length > 0)
                {
                    var filePath = await _fileService.SavePhotoAsync(file);
                    _context.Photos.Add(new Photo
                    {
                        FileName = file.FileName,
                        FilePath = filePath,
                        FileSize = file.Length,
                        AppealId = appeal.Id
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetAppeal), new { id = appeal.Id }, appeal);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Deputy,Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var appeal = await _context.Appeals.FindAsync(id);
        if (appeal == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        var roles = await _userManager.GetRolesAsync(user!);
        if (roles.Contains("Deputy") && appeal.DistrictId != user!.AssignedDistrictId)
            return Forbid();

        var oldStatus = appeal.Status;
        appeal.Status = dto.NewStatus;
        appeal.UpdatedAt = DateTime.UtcNow;

        var response = new AppealResponse
        {
            AppealId = appeal.Id,
            AuthorId = userId!,
            Content = $"Статус изменён с {oldStatus} на {dto.NewStatus}.",
            IsSystem = true
        };
        _context.AppealResponses.Add(response);

        await _context.SaveChangesAsync();
        return Ok(appeal);
    }

    [HttpPost("{id}/respond")]
    [Authorize(Roles = "Deputy,Admin")]
    public async Task<IActionResult> AddResponse(int id, [FromBody] AddResponseDto dto)
    {
        var appeal = await _context.Appeals.FindAsync(id);
        if (appeal == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        var roles = await _userManager.GetRolesAsync(user!);
        if (roles.Contains("Deputy") && appeal.DistrictId != user!.AssignedDistrictId)
            return Forbid();

        var response = new AppealResponse
        {
            AppealId = id,
            AuthorId = userId!,
            Content = dto.Content,
            IsSystem = false
        };
        _context.AppealResponses.Add(response);
        await _context.SaveChangesAsync();

        return Ok(response);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAppeal(int id)
    {
        var appeal = await _context.Appeals
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appeal == null) return NotFound();

        foreach (var photo in appeal.Photos)
        {
            _fileService.DeletePhoto(photo.FilePath);
        }

        _context.Appeals.Remove(appeal);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private Point? ParsePoint(string geoJson)
    {
        try
        {
            var reader = new GeoJsonReader();
            var geom = reader.Read<Geometry>(geoJson);
            return geom as Point;
        }
        catch
        {
            return null;
        }
    }
}