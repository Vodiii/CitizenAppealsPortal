using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Hubs;
using CitizenAppealsPortal.Models;
using CitizenAppealsPortal.Models.DTOs;
using CitizenAppealsPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly GeoJsonWriter _geoJsonWriter;
    private readonly IHubContext<NotificationHub>? _hubContext;  // может быть null, если SignalR не подключён
    private readonly ILogger<AppealsController> _logger;

    public AppealsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IGeoService geoService,
        IFileService fileService,
        IHubContext<NotificationHub>? hubContext,
        ILogger<AppealsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _geoService = geoService;
        _fileService = fileService;
        _geoJsonWriter = new GeoJsonWriter();
        _hubContext = hubContext;
        _logger = logger;
    }

    // ========== Приватные хелперы ==========

    /// <summary>
    /// Добавляет уведомление в контекст и, если доступен SignalR, отправляет его получателю в реальном времени.
    /// </summary>
    private async Task SendNotificationAsync(string userId, int? appealId, string type, string message)
    {
        var notification = new Notification
        {
            UserId = userId,
            AppealId = appealId,
            Type = type,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();  // сохраняем, чтобы получить Id

        // Отправка через SignalR (если хаб доступен)
        if (_hubContext != null)
        {
            try
            {
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    type = notification.Type,
                    message = notification.Message,
                    appealId = notification.AppealId,
                    createdAt = notification.CreatedAt,
                    isRead = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось отправить SignalR-уведомление пользователю {UserId}", userId);
            }
        }
    }

    // ========== Эндпоинты ==========

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
            .Include(a => a.Votes)
            .AsQueryable();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        var roles = await _userManager.GetRolesAsync(user!);

        if (roles.Contains(RoleNames.Citizen))
            query = query.Where(a => a.CitizenId == userId);
        else if (roles.Contains(RoleNames.Deputy))
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

        var orderedQuery = roles.Contains(RoleNames.Deputy)
            ? query.OrderByDescending(a => a.Score).ThenByDescending(a => a.CreatedAt)
            : query.OrderByDescending(a => a.CreatedAt);

        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AppealDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                Address = a.Address,
                LocationGeoJson = _geoJsonWriter.Write(a.Location),
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                Status = a.Status,
                CitizenId = a.CitizenId,
                CitizenFullName = a.Citizen.FullName,
                CategoryId = a.CategoryId,
                CategoryName = a.Category.Name,
                DistrictId = a.DistrictId,
                DistrictName = a.District.Name,
                Score = a.Score,
                UpVotes = a.Votes.Count(v => v.VoteType == 1),
                DownVotes = a.Votes.Count(v => v.VoteType == -1),
                UserVote = a.Votes.Where(v => v.UserId == userId).Select(v => (int?)v.VoteType).FirstOrDefault(),
                Photos = a.Photos.Select(p => new PhotoDto
                {
                    Id = p.Id,
                    FileName = p.FileName,
                    FilePath = p.FilePath
                }).ToList()
            })
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
            .Include(a => a.Votes)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appeal == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        var roles = await _userManager.GetRolesAsync(user!);

        if (roles.Contains(RoleNames.Citizen) && appeal.CitizenId != userId)
            return Forbid();
        if (roles.Contains(RoleNames.Deputy) && appeal.DistrictId != user!.AssignedDistrictId)
            return Forbid();

        var dto = new AppealDto
        {
            Id = appeal.Id,
            Title = appeal.Title,
            Description = appeal.Description,
            Address = appeal.Address,
            LocationGeoJson = _geoJsonWriter.Write(appeal.Location),
            CreatedAt = appeal.CreatedAt,
            UpdatedAt = appeal.UpdatedAt,
            Status = appeal.Status,
            CitizenId = appeal.CitizenId,
            CitizenFullName = appeal.Citizen.FullName,
            CategoryId = appeal.CategoryId,
            CategoryName = appeal.Category.Name,
            DistrictId = appeal.DistrictId,
            DistrictName = appeal.District.Name,
            Score = appeal.Score,
            UpVotes = appeal.Votes.Count(v => v.VoteType == 1),
            DownVotes = appeal.Votes.Count(v => v.VoteType == -1),
            UserVote = appeal.Votes.Where(v => v.UserId == userId).Select(v => (int?)v.VoteType).FirstOrDefault(),
            Photos = appeal.Photos.Select(p => new PhotoDto
            {
                Id = p.Id,
                FileName = p.FileName,
                FilePath = p.FilePath
            }).ToList(),
            Responses = appeal.Responses.OrderBy(r => r.CreatedAt).Select(r => new AppealResponseDto
            {
                Id = r.Id,
                Content = r.Content,
                CreatedAt = r.CreatedAt,
                IsSystem = r.IsSystem,
                ResponseType = r.ResponseType,
                AuthorFullName = r.Author.FullName
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Citizen)]
    public async Task<IActionResult> CreateAppeal([FromForm] CreateAppealDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);

        var point = ParsePoint(dto.LocationGeoJson);
        if (point == null) return BadRequest("Некорректные координаты.");

        var districtId = await _geoService.FindDistrictIdByPointAsync(point);
        if (districtId == null)
            return BadRequest("Не удалось определить округ для указанного местоположения.");

        var category = await _context.Categories.FindAsync(dto.CategoryId);
        if (category == null) return BadRequest("Категория не найдена.");

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

        var createdAppeal = await _context.Appeals
            .Include(a => a.Category).Include(a => a.District).Include(a => a.Citizen).Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == appeal.Id);

        var dtoResponse = new AppealDto
        {
            Id = createdAppeal!.Id,
            Title = createdAppeal.Title,
            Description = createdAppeal.Description,
            Address = createdAppeal.Address,
            LocationGeoJson = _geoJsonWriter.Write(createdAppeal.Location),
            CreatedAt = createdAppeal.CreatedAt,
            UpdatedAt = createdAppeal.UpdatedAt,
            Status = createdAppeal.Status,
            CitizenId = createdAppeal.CitizenId,
            CitizenFullName = createdAppeal.Citizen.FullName,
            CategoryId = createdAppeal.CategoryId,
            CategoryName = createdAppeal.Category.Name,
            DistrictId = createdAppeal.DistrictId,
            DistrictName = createdAppeal.District.Name,
            Photos = createdAppeal.Photos.Select(p => new PhotoDto
            {
                Id = p.Id,
                FileName = p.FileName,
                FilePath = p.FilePath
            }).ToList()
        };

        return CreatedAtAction(nameof(GetAppeal), new { id = appeal.Id }, dtoResponse);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = RoleNames.Deputy + "," + RoleNames.Admin)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var appeal = await _context.Appeals.FindAsync(id);
        if (appeal == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        var roles = await _userManager.GetRolesAsync(user!);
        if (roles.Contains(RoleNames.Deputy) && appeal.DistrictId != user!.AssignedDistrictId)
            return Forbid();

        var oldStatus = appeal.Status;
        appeal.Status = dto.NewStatus;
        appeal.UpdatedAt = DateTime.UtcNow;

        var response = new AppealResponse
        {
            AppealId = appeal.Id,
            AuthorId = userId!,
            Content = $"Статус изменён с {oldStatus} на {dto.NewStatus}.",
            IsSystem = true,
            ResponseType = ResponseType.System
        };
        _context.AppealResponses.Add(response);

        // Уведомление автору
        await SendNotificationAsync(appeal.CitizenId, appeal.Id,
            NotificationType.StatusChange,
            $"Статус вашего обращения «{appeal.Title}» изменён на {dto.NewStatus}.");

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Статус обновлён" });
    }

    [HttpPost("{id}/respond")]
    [Authorize(Roles = RoleNames.Deputy + "," + RoleNames.Admin)]
    public async Task<IActionResult> AddResponse(int id, [FromBody] AddResponseDto dto)
    {
        var appeal = await _context.Appeals.FindAsync(id);
        if (appeal == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        var roles = await _userManager.GetRolesAsync(user!);
        if (roles.Contains(RoleNames.Deputy) && appeal.DistrictId != user!.AssignedDistrictId)
            return Forbid();

        var response = new AppealResponse
        {
            AppealId = id,
            AuthorId = userId!,
            Content = dto.Content,
            IsSystem = false,
            ResponseType = ResponseType.Normal
        };
        _context.AppealResponses.Add(response);

        var snippet = dto.Content.Length > 50 ? dto.Content[..50] + "..." : dto.Content;
        await SendNotificationAsync(appeal.CitizenId, appeal.Id,
            NotificationType.NewResponse,
            $"Новый ответ по обращению «{appeal.Title}»: {snippet}");

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Ответ добавлен" });
    }

    [HttpPost("{id}/reopen")]
    [Authorize(Roles = RoleNames.Citizen)]
    public async Task<IActionResult> ReopenAppeal(int id, [FromBody] ReopenAppealDto dto)
    {
        var appeal = await _context.Appeals.FindAsync(id);
        if (appeal == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (appeal.CitizenId != userId) return Forbid();

        if (appeal.Status != AppealStatus.Completed && appeal.Status != AppealStatus.Rejected)
            return BadRequest("Обращение можно возобновить только после завершения или отклонения.");

        appeal.Status = AppealStatus.New;
        appeal.UpdatedAt = DateTime.UtcNow;

        var response = new AppealResponse
        {
            AppealId = id,
            AuthorId = userId!,
            Content = dto.Message,
            ResponseType = ResponseType.Reopen,
            IsSystem = false
        };
        _context.AppealResponses.Add(response);

        // Уведомление всем активным депутатам этого округа
        var districtDeputies = await _userManager.Users
            .Where(u => u.AssignedDistrictId == appeal.DistrictId
                        && _context.DeputyTerms.Any(t => t.DeputyId == u.Id && t.IsActive))
            .ToListAsync();

        foreach (var dep in districtDeputies)
        {
            await SendNotificationAsync(dep.Id, appeal.Id,
                NotificationType.Reopen,
                $"Обращение «{appeal.Title}» возобновлено гражданином: {dto.Message}");
        }

        // Сохраняем оставшиеся изменения
        await _context.SaveChangesAsync();

        // Возвращаем обновлённое обращение (код повторяющийся, но для ясности оставлен)
        var createdAppeal = await _context.Appeals
            .Include(a => a.Category).Include(a => a.District).Include(a => a.Citizen)
            .Include(a => a.Photos).Include(a => a.Responses).ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(a => a.Id == appeal.Id);

        var dtoResponse = new AppealDto
        {
            Id = createdAppeal!.Id,
            Title = createdAppeal.Title,
            Description = createdAppeal.Description,
            Address = createdAppeal.Address,
            LocationGeoJson = _geoJsonWriter.Write(createdAppeal.Location),
            CreatedAt = createdAppeal.CreatedAt,
            UpdatedAt = createdAppeal.UpdatedAt,
            Status = createdAppeal.Status,
            CitizenId = createdAppeal.CitizenId,
            CitizenFullName = createdAppeal.Citizen.FullName,
            CategoryId = createdAppeal.CategoryId,
            CategoryName = createdAppeal.Category.Name,
            DistrictId = createdAppeal.DistrictId,
            DistrictName = createdAppeal.District.Name,
            Photos = createdAppeal.Photos.Select(p => new PhotoDto
            {
                Id = p.Id,
                FileName = p.FileName,
                FilePath = p.FilePath
            }).ToList(),
            Responses = createdAppeal.Responses.OrderBy(r => r.CreatedAt).Select(r => new AppealResponseDto
            {
                Id = r.Id,
                Content = r.Content,
                CreatedAt = r.CreatedAt,
                IsSystem = r.IsSystem,
                ResponseType = r.ResponseType,
                AuthorFullName = r.Author.FullName
            }).ToList()
        };

        return Ok(dtoResponse);
    }

    [HttpPost("{id}/vote")]
    [Authorize]
    public async Task<IActionResult> VoteAppeal(int id, [FromBody] VoteDto dto)
    {
        if (dto.VoteType != 1 && dto.VoteType != -1)
            return BadRequest("VoteType должен быть 1 или -1.");

        var appeal = await _context.Appeals.Include(a => a.Votes).FirstOrDefaultAsync(a => a.Id == id);
        if (appeal == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var existingVote = appeal.Votes.FirstOrDefault(v => v.UserId == userId);
        int finalVoteType;

        if (existingVote != null)
        {
            if (existingVote.VoteType == dto.VoteType)
            {
                // Отмена голоса
                appeal.Score -= existingVote.VoteType;
                _context.AppealVotes.Remove(existingVote);
                finalVoteType = 0;
            }
            else
            {
                // Замена голоса
                appeal.Score -= existingVote.VoteType;
                existingVote.VoteType = dto.VoteType;
                appeal.Score += dto.VoteType;
                finalVoteType = dto.VoteType;
            }
        }
        else
        {
            _context.AppealVotes.Add(new AppealVote
            {
                AppealId = id,
                UserId = userId,
                VoteType = dto.VoteType
            });
            appeal.Score += dto.VoteType;
            finalVoteType = dto.VoteType;
        }

        if (finalVoteType != 0)
        {
            await SendNotificationAsync(appeal.CitizenId, appeal.Id,
                NotificationType.NewVote,
                finalVoteType == 1
                    ? $"Ваше обращение «{appeal.Title}» получило голос 👍. Текущий рейтинг: {appeal.Score}"
                    : $"Ваше обращение «{appeal.Title}» получило голос 👎. Текущий рейтинг: {appeal.Score}");
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            score = appeal.Score,
            upVotes = appeal.Votes.Count(v => v.VoteType == 1),
            downVotes = appeal.Votes.Count(v => v.VoteType == -1),
            userVote = finalVoteType
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> DeleteAppeal(int id)
    {
        var appeal = await _context.Appeals.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == id);
        if (appeal == null) return NotFound();

        foreach (var photo in appeal.Photos)
            _fileService.DeletePhoto(photo.FilePath);

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

