using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;

namespace PlatformFlower.Controllers.ProfileUser
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class TestController : ControllerBase
    {
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public TestController(
            IResponseService responseService,
            IAppLogger logger)
        {
            _responseService = responseService;
            _logger = logger;
        }

        /// <summary>
        /// Test Cloudinary connection (Development only)
        /// </summary>
        /// <returns>Cloudinary connection status</returns>
        [HttpGet("test-cloudinary-config")]
        public ActionResult<ApiResponse<object>> TestCloudinaryConfig()
        {
            try
            {
                var cloudinaryInfo = new
                {
                    CloudNameConfigured = !string.IsNullOrEmpty(HttpContext.RequestServices
                        .GetService<PlatformFlower.Services.Common.Configuration.ICloudinaryConfiguration>()?.CloudName),
                    ApiKeyConfigured = !string.IsNullOrEmpty(HttpContext.RequestServices
                        .GetService<PlatformFlower.Services.Common.Configuration.ICloudinaryConfiguration>()?.ApiKey),
                    ApiSecretConfigured = !string.IsNullOrEmpty(HttpContext.RequestServices
                        .GetService<PlatformFlower.Services.Common.Configuration.ICloudinaryConfiguration>()?.ApiSecret)
                };

                var response = _responseService.CreateSuccessResponse(cloudinaryInfo, "Cloudinary configuration check");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cloudinary config test failed: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<object>($"Cloudinary config test failed: {ex.Message}");
                return StatusCode(500, response);
            }
        }
    }
}
