using Microsoft.Extensions.Logging;

namespace CitizenAppealsPortal.Services;

public class FileService : IFileService
{
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private static readonly string[] DocumentExtensions = { ".pdf", ".doc", ".docx" };

    private readonly IWebHostEnvironment _env;
    private readonly string _uploadFolder;
    private readonly ILogger<FileService> _logger;

    public FileService(IWebHostEnvironment env, IConfiguration config, ILogger<FileService> logger)
    {
        _env = env;
        _logger = logger;
        _uploadFolder = Path.Combine(env.WebRootPath ?? "wwwroot",
                                     config["FileStorage:UploadPath"] ?? "uploads");
        if (!Directory.Exists(_uploadFolder))
        {
            Directory.CreateDirectory(_uploadFolder);
            _logger.LogInformation("Папка загрузок создана: {UploadFolder}", _uploadFolder);
        }
    }

    public Task<string> SavePhotoAsync(IFormFile file)
        => SaveFileAsync(file, ImageExtensions, "Разрешены только изображения (jpg, png, gif).");

    public Task<string> SaveDocumentAsync(IFormFile file)
        => SaveFileAsync(file, DocumentExtensions, "Разрешены изображения и документы PDF/DOC.");

    private async Task<string> SaveFileAsync(IFormFile file, string[] allowedExtensions, string errorMessage)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Попытка загрузить пустой файл");
            throw new ArgumentException("Файл не выбран или пуст.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
        {
            _logger.LogWarning("Недопустимое расширение файла: {FileName}", file.FileName);
            throw new ArgumentException(errorMessage);
        }

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(_uploadFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation("Файл {FileName} сохранён как {SavedAs}", file.FileName, fileName);

        return Path.Combine("uploads", fileName).Replace("\\", "/");
    }

    public void DeletePhoto(string filePath)
    {
        var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Файл удалён: {FilePath}", filePath);
        }
        else
        {
            _logger.LogWarning("Файл не найден для удаления: {FilePath}", filePath);
        }
    }
}