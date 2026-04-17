namespace CitizenAppealsPortal.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly string _uploadFolder;

    public FileService(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _uploadFolder = Path.Combine(env.WebRootPath ?? "wwwroot", config["FileStorage:UploadPath"] ?? "uploads");
        if (!Directory.Exists(_uploadFolder))
            Directory.CreateDirectory(_uploadFolder);
    }

    public async Task<string> SavePhotoAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Файл не выбран или пуст.");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            throw new ArgumentException("Недопустимый формат файла. Разрешены только изображения.");

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(_uploadFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Path.Combine("uploads", fileName).Replace("\\", "/");
    }

    public void DeletePhoto(string filePath)
    {
        var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", filePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}