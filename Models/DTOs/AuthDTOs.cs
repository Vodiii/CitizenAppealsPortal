using System.ComponentModel.DataAnnotations;

namespace CitizenAppealsPortal.Models.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать не менее 6 символов")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "ФИО обязательно")]
    [MaxLength(100, ErrorMessage = "ФИО не должно превышать 100 символов")]
    public string FullName { get; set; } = string.Empty;

    public string? Role { get; set; }
}

public class LoginDto
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    public string Password { get; set; } = string.Empty;
}