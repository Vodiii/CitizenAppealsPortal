namespace CitizenAppealsPortal.Services;

public interface IFileService
{
    Task<string> SavePhotoAsync(IFormFile file);
    void DeletePhoto(string filePath);
}