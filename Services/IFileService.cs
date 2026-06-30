namespace CitizenAppealsPortal.Services;

public interface IFileService
{
    Task<string> SavePhotoAsync(IFormFile file);
    Task<string> SaveDocumentAsync(IFormFile file);
    void DeletePhoto(string filePath);
}