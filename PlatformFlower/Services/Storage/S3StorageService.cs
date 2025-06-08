using Amazon.S3;
using Amazon.S3.Model;
using PlatformFlower.Services.Common.Configuration;
using PlatformFlower.Services.Common.Logging;

namespace PlatformFlower.Services.Storage
{
    public class S3StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IAwsConfiguration _awsConfig;
        private readonly IAppLogger _logger;

        public S3StorageService(IAmazonS3 s3Client, IAwsConfiguration awsConfig, IAppLogger logger)
        {
            _s3Client = s3Client;
            _awsConfig = awsConfig;
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            try
            {
                _logger.LogInformation($"Starting file upload. Bucket: {_awsConfig.BucketName}, Region: {_awsConfig.Region}");

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
                var key = $"{folder.Trim('/')}/{fileName}";

                _logger.LogInformation($"Uploading file to S3: {key}, Size: {file.Length} bytes");

                using var stream = file.OpenReadStream();
                
                var request = new PutObjectRequest
                {
                    BucketName = _awsConfig.BucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = file.ContentType
                    // Remove CannedACL since bucket blocks ACLs
                };

                var response = await _s3Client.PutObjectAsync(request);

                _logger.LogInformation($"S3 Response Status: {response.HttpStatusCode}");

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Try direct URL first (if bucket has public read policy)
                    var fileUrl = $"https://{_awsConfig.BucketName}.s3.{_awsConfig.Region}.amazonaws.com/{key}";
                    _logger.LogInformation($"File uploaded successfully: {fileUrl}");

                    // Alternative: Generate presigned URL for private buckets
                    // var presignedUrl = await _s3Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
                    // {
                    //     BucketName = _awsConfig.BucketName,
                    //     Key = key,
                    //     Verb = HttpVerb.GET,
                    //     Expires = DateTime.UtcNow.AddYears(1)
                    // });

                    return fileUrl;
                }
                else
                {
                    _logger.LogError($"S3 upload failed with status: {response.HttpStatusCode}");
                    throw new InvalidOperationException($"Failed to upload file. Status: {response.HttpStatusCode}");
                }
            }
            catch (Amazon.S3.AmazonS3Exception s3Ex)
            {
                _logger.LogError($"AWS S3 Error: {s3Ex.ErrorCode} - {s3Ex.Message}", s3Ex);
                throw new InvalidOperationException($"S3 Error: {s3Ex.ErrorCode} - {s3Ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading file to S3: {ex.Message}", ex);
                throw new InvalidOperationException($"Upload failed: {ex.Message}");
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return false;

                // Extract key from URL
                var key = ExtractKeyFromUrl(fileUrl);
                if (string.IsNullOrEmpty(key))
                    return false;

                _logger.LogInformation($"Deleting file from S3: {key}");

                var request = new DeleteObjectRequest
                {
                    BucketName = _awsConfig.BucketName,
                    Key = key
                };

                var response = await _s3Client.DeleteObjectAsync(request);
                
                _logger.LogInformation($"File deleted successfully: {key}");
                return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting file from S3: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return false;

                var key = ExtractKeyFromUrl(fileUrl);
                if (string.IsNullOrEmpty(key))
                    return false;

                var request = new GetObjectMetadataRequest
                {
                    BucketName = _awsConfig.BucketName,
                    Key = key
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking file existence in S3: {ex.Message}", ex);
                return false;
            }
        }

        private string ExtractKeyFromUrl(string fileUrl)
        {
            try
            {
                // Extract key from URL like: https://bucket.s3.region.amazonaws.com/folder/filename.jpg
                var uri = new Uri(fileUrl);
                return uri.AbsolutePath.TrimStart('/');
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
