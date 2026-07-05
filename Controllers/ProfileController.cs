using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using CitizenAppealsPortal.Models.DTOs;
using CitizenAppealsPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CitizenAppealsPortal.Controllers;

[Authorize]
[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IFileService fileService,
        ILogger<ProfileController> logger)
    {
        _userManager = userManager;
        _context = context;
        _fileService = fileService;
        _logger = logger;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ========== Основное ==========
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        var dto = new ProfileDto
        {
            Email = user.Email!,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth
        };

        if (user.AssignedDistrictId.HasValue)
        {
            var district = await _context.Districts
                .Include(d => d.Deputies)
                .FirstOrDefaultAsync(d => d.Id == user.AssignedDistrictId);

            if (district != null)
            {
                var activeDeputy = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.AssignedDistrictId == district.Id
                                              && u.IsApproved
                                              && _context.DeputyTerms.Any(t => t.DeputyId == u.Id && t.IsActive));

                dto.Deputy = new DeputyInfoDto
                {
                    DistrictId = district.Id,
                    DistrictName = district.Name,
                    DeputyFullName = activeDeputy?.FullName,
                    DeputyEmail = activeDeputy?.Email,
                    DeputyPhone = activeDeputy?.PhoneNumber,
                    IsActiveTerm = activeDeputy != null
                };
            }
        }

        return Ok(dto);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        user.DateOfBirth = dto.DateOfBirth;
        await _userManager.UpdateAsync(user);
        _logger.LogInformation("Профиль пользователя {UserId} обновлён", UserId);
        return NoContent();
    }

    // ========== Безопасность ==========
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Неудачная попытка смены пароля для пользователя {UserId}", UserId);
            return BadRequest(result.Errors);
        }

        _logger.LogInformation("Пароль пользователя {UserId} изменён", UserId);
        return NoContent();
    }

    [HttpGet("login-history")]
    public async Task<IActionResult> GetLoginHistory()
    {
        var history = await _context.UserLoginHistories
            .Where(h => h.UserId == UserId)
            .OrderByDescending(h => h.LoginTime)
            .Select(h => new LoginHistoryDto
            {
                LoginTime = h.LoginTime,
                IpAddress = h.IpAddress,
                UserAgent = h.UserAgent
            })
            .ToListAsync();

        return Ok(history);
    }

    // ========== Документы ==========
    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments()
    {
        var docs = await _context.UserDocuments
            .Where(d => d.UserId == UserId)
            .Select(d => new UserDocumentDto
            {
                Id = d.Id,
                DocumentType = d.DocumentType,
                FileName = d.FileName,
                FilePath = d.FilePath,
                UploadedAt = d.UploadedAt
            })
            .ToListAsync();
        return Ok(docs);
    }

    [HttpPost("documents")]
    public async Task<IActionResult> UploadDocument([FromForm] CreateDocumentDto dto)
    {
        var filePath = await _fileService.SaveDocumentAsync(dto.File);
        var doc = new UserDocument
        {
            UserId = UserId,
            DocumentType = dto.DocumentType,
            FileName = dto.File.FileName,
            FilePath = filePath,
            UploadedAt = DateTime.UtcNow
        };
        _context.UserDocuments.Add(doc);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Документ загружен пользователем {UserId}: {FileName}", UserId, dto.File.FileName);
        return CreatedAtAction(nameof(GetDocuments), new { id = doc.Id }, new UserDocumentDto
        {
            Id = doc.Id,
            DocumentType = doc.DocumentType,
            FileName = doc.FileName,
            FilePath = filePath,
            UploadedAt = doc.UploadedAt
        });
    }

    [HttpDelete("documents/{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var doc = await _context.UserDocuments.FirstOrDefaultAsync(d => d.Id == id && d.UserId == UserId);
        if (doc == null) return NotFound();
        _fileService.DeletePhoto(doc.FilePath);
        _context.UserDocuments.Remove(doc);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Документ {DocumentId} удалён пользователем {UserId}", id, UserId);
        return NoContent();
    }

    // ========== Настройки ==========
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _context.UserSettings
            .Where(s => s.UserId == UserId)
            .Select(s => new UserSettingDto { Key = s.Key, Value = s.Value })
            .ToListAsync();
        return Ok(settings);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsDto dto)
    {
        var existing = await _context.UserSettings.Where(s => s.UserId == UserId).ToListAsync();
        foreach (var setting in dto.Settings)
        {
            var exist = existing.Find(e => e.Key == setting.Key);
            if (exist != null) exist.Value = setting.Value;
            else _context.UserSettings.Add(new UserSetting { UserId = UserId, Key = setting.Key, Value = setting.Value });
        }
        await _context.SaveChangesAsync();
        _logger.LogInformation("Настройки пользователя {UserId} обновлены", UserId);
        return NoContent();
    }

    // ========== Архив обращений ==========
    [HttpGet("archived-appeals")]
    public async Task<IActionResult> GetArchivedAppeals()
    {
        var appeals = await _context.Appeals
            .Include(a => a.Category)
            .Include(a => a.District)
            .Where(a => a.CitizenId == UserId && (a.Status == AppealStatus.Completed || a.Status == AppealStatus.Rejected))
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AppealDto
            {
                Id = a.Id,
                Title = a.Title,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                CategoryName = a.Category.Name,
                DistrictName = a.District.Name
            })
            .ToListAsync();

        return Ok(appeals);
    }

    // ========== Подписки на категории ==========
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions()
    {
        var subscribedIds = await _context.UserCategorySubscriptions
            .Where(s => s.UserId == UserId)
            .Select(s => s.CategoryId)
            .ToListAsync();

        var allCategories = await _context.Categories
            .Select(c => new CategorySubscriptionDto
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                Subscribed = subscribedIds.Contains(c.Id)
            })
            .ToListAsync();

        return Ok(allCategories);
    }

    [HttpPut("subscriptions")]
    public async Task<IActionResult> UpdateSubscriptions([FromBody] UpdateSubscriptionsDto dto)
    {
        var existing = await _context.UserCategorySubscriptions
            .Where(s => s.UserId == UserId)
            .ToListAsync();

        var toRemove = existing.Where(s => !dto.CategoryIds.Contains(s.CategoryId));
        _context.UserCategorySubscriptions.RemoveRange(toRemove);

        var existingIds = existing.Select(s => s.CategoryId).ToHashSet();
        foreach (var catId in dto.CategoryIds)
        {
            if (!existingIds.Contains(catId))
            {
                _context.UserCategorySubscriptions.Add(new UserCategorySubscription
                {
                    UserId = UserId,
                    CategoryId = catId,
                    SubscribedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Подписки пользователя {UserId} обновлены", UserId);
        return NoContent();
    }
}