using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using PlatformFlower.Services.Common.Configuration;
using PlatformFlower.Services.Common.Logging;

namespace PlatformFlower.Services.Storage
{
    public class CloudinaryStorageService : IStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ICloudinaryConfiguration _cloudinaryConfig;
        private readonly IAppLogger _logger;

        public CloudinaryStorageService(
            ICloudinaryConfiguration cloudinaryConfig,
            IAppLogger logger)
        {
            _cloudinaryConfig = cloudinaryConfig;
            _logger = logger;

            // Initialize Cloudinary
            var account = new Account(
                _cloudinaryConfig.CloudName,
                _cloudinaryConfig.ApiKey,
                _cloudinaryConfig.ApiSecret
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            try
            {
                _logger.LogInformation($"Starting file upload to Cloudinary. Cloud: {_cloudinaryConfig.CloudName}");
                
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty or null");

                // Validate file type (images only)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                    throw new ArgumentException($"File type {fileExtension} is not allowed. Only images are supported.");

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                    throw new ArgumentException("File size cannot exceed 5MB");

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var publicId = $"{folder.Trim('/')}/{Path.GetFileNameWithoutExtension(fileName)}";

                _logger.LogInformation($"Uploading file to Cloudinary: {publicId}, Size: {file.Length} bytes");

                using var stream = file.OpenReadStream();
                
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(fileName, stream),
                    PublicId = publicId,
                    Folder = folder.Trim('/'),
                    Overwrite = true,
                    Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                
                _logger.LogInformation($"Cloudinary Response Status: {uploadResult.StatusCode}");
                
                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var fileUrl = uploadResult.SecureUrl.ToString();
                    _logger.LogInformation($"File uploaded successfully: {fileUrl}");
                    return fileUrl;
                }
                else
                {
                    _logger.LogError($"Cloudinary upload failed with status: {uploadResult.StatusCode}, Error: {uploadResult.Error?.Message}");
                    throw new InvalidOperationException($"Failed to upload file. Status: {uploadResult.StatusCode}, Error: {uploadResult.Error?.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading file to Cloudinary: {ex.Message}", ex);
                throw new InvalidOperationException($"Upload failed: {ex.Message}");
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return true; // Nothing to delete

                var publicId = ExtractPublicIdFromUrl(fileUrl);
                if (string.IsNullOrEmpty(publicId))
                {
                    _logger.LogWarning($"Could not extract public ID from URL: {fileUrl}");
                    return false;
                }

                _logger.LogInformation($"Deleting file from Cloudinary: {publicId}");

                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);
                
                if (result.Result == "ok")
                {
                    _logger.LogInformation($"File deleted successfully: {publicId}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Failed to delete file: {publicId}, Result: {result.Result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting file from Cloudinary: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return false;

                var publicId = ExtractPublicIdFromUrl(fileUrl);
                if (string.IsNullOrEmpty(publicId))
                    return false;

                var getResourceParams = new GetResourceParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };

                var result = await _cloudinary.GetResourceAsync(getResourceParams);
                return result != null && !string.IsNullOrEmpty(result.PublicId);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string ExtractPublicIdFromUrl(string fileUrl)
        {
            try
            {
                // Cloudinary URL format: https://res.cloudinary.com/{cloud_name}/image/upload/v{version}/{public_id}.{format}
                var uri = new Uri(fileUrl);
                var pathSegments = uri.AbsolutePath.Split('/');
                
                // Find the upload segment
                var uploadIndex = Array.IndexOf(pathSegments, "upload");
                if (uploadIndex == -1 || uploadIndex + 2 >= pathSegments.Length)
                    return string.Empty;

                // Get everything after version (v{number})
                var publicIdParts = pathSegments.Skip(uploadIndex + 2).ToArray();
                var publicIdWithExtension = string.Join("/", publicIdParts);
                
                // Remove file extension
                var lastDotIndex = publicIdWithExtension.LastIndexOf('.');
                if (lastDotIndex > 0)
                    return publicIdWithExtension.Substring(0, lastDotIndex);
                
                return publicIdWithExtension;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
