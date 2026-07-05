using CitizenAppealsPortal.Data;
using CitizenAppealsPortal.Models;
using CitizenAppealsPortal.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CitizenAppealsPortal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthController> _logger;

    // Имена claim'ов как константы, чтобы избежать опечаток
    private const string ClaimFullName = "fullName";

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ApplicationDbContext context,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Ошибка регистрации пользователя {Email}: {Errors}",
                model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(result.Errors);
        }

        // Нормализация роли: если роль не указана или указана некорректно, назначаем Citizen
        var role = model.Role switch
        {
            RoleNames.Deputy => RoleNames.Deputy,
            _ => RoleNames.Citizen
        };

        await _userManager.AddToRoleAsync(user, role);

        if (role == RoleNames.Deputy)
        {
            user.IsApproved = false;  // депутат требует подтверждения администратором
        }
        else
        {
            user.IsApproved = true;   // граждане подтверждены по умолчанию
        }

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Пользователь {Email} зарегистрирован с ролью {Role}", model.Email, role);
        return Ok(new { Message = "Регистрация успешна" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            _logger.LogWarning("Неудачная попытка входа для {Email}", model.Email);
            return Unauthorized("Неверный email или пароль.");
        }

        // Проверка и актуализация роли депутата по сроку полномочий
        await EnsureDeputyRoleValidityAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        // Запись истории входа (без прерывания входа при ошибке записи)
        try
        {
            _context.UserLoginHistories.Add(new UserLoginHistory
            {
                UserId = user.Id,
                LoginTime = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString()
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось записать историю входа для пользователя {UserId}", user.Id);
        }

        var token = GenerateJwtToken(user, roles);
        _logger.LogInformation("Пользователь {Email} вошёл в систему", model.Email);
        return Ok(new { Token = token, Roles = roles });
    }

    /// <summary>
    /// Проверяет, есть ли у пользователя активный срок депутата, и соответствующим образом корректирует его роли.
    /// </summary>
    private async Task EnsureDeputyRoleValidityAsync(ApplicationUser user)
    {
        bool isDeputy = await _userManager.IsInRoleAsync(user, RoleNames.Deputy);
        var now = DateTime.UtcNow;

        bool hasActiveTerm = await _context.DeputyTerms
            .AnyAsync(t => t.DeputyId == user.Id && t.IsActive && t.StartDate <= now && t.EndDate >= now);

        if (isDeputy && !hasActiveTerm)
        {
            // Срок истёк или депутат не имеет активного срока — лишаем роли
            await _userManager.RemoveFromRoleAsync(user, RoleNames.Deputy);
            if (!await _userManager.IsInRoleAsync(user, RoleNames.Citizen))
                await _userManager.AddToRoleAsync(user, RoleNames.Citizen);

            _logger.LogInformation("Роль депутата снята с пользователя {UserId} (нет активного срока)", user.Id);
        }
        else if (!isDeputy && hasActiveTerm)
        {
            // Срок активен, но роль отсутствует — восстанавливаем
            await _userManager.AddToRoleAsync(user, RoleNames.Deputy);
            // Убираем Citizen, если он был, чтобы не было двойной роли
            if (await _userManager.IsInRoleAsync(user, RoleNames.Citizen))
                await _userManager.RemoveFromRoleAsync(user, RoleNames.Citizen);

            _logger.LogInformation("Роль депутата восстановлена для пользователя {UserId} (активный срок)", user.Id);
        }
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var expireDays = Convert.ToDouble(jwtSection["ExpireDays"]);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimFullName, user.FullName)  // используем константу
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expireDays),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}