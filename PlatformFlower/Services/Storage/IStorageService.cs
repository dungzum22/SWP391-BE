namespace PlatformFlower.Services.Storage
{
    public interface IStorageService
    {
        /// <summary>
        /// Upload file to S3 and return the public URL
        /// </summary>
        /// <param name="file">File to upload</param>
        /// <param name="folder">Folder path in S3 (e.g., "avatars", "products")</param>
        /// <returns>Public URL of uploaded file</returns>
        Task<string> UploadFileAsync(IFormFile file, string folder);

        /// <summary>
        /// Delete file from S3
        /// </summary>
        /// <param name="fileUrl">Full URL of the file to delete</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteFileAsync(string fileUrl);

        /// <summary>
        /// Check if file exists in S3
        /// </summary>
        /// <param name="fileUrl">Full URL of the file</param>
        /// <returns>True if file exists</returns>
        Task<bool> FileExistsAsync(string fileUrl);
    }
}
