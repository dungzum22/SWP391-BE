namespace PlatformFlower.Services.Storage
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folder);

        Task<bool> DeleteFileAsync(string fileUrl);

        Task<bool> FileExistsAsync(string fileUrl);
    }
}
