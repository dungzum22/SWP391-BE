using Microsoft.IdentityModel.Tokens;
using PlatformFlower.Models.DTOs.User;
using PlatformFlower.Services.Common.Configuration;
using PlatformFlower.Services.Common.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PlatformFlower.Services.Auth
{
    public class JwtService : IJwtService
    {
        private readonly IJwtConfiguration _jwtConfig;
        private readonly IAppLogger _logger;

        public JwtService(IJwtConfiguration jwtConfig, IAppLogger logger)
        {
            _jwtConfig = jwtConfig;
            _logger = logger;
        }

        public string GenerateToken(UserResponse user)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Type),
                    new Claim("user_id", user.UserId.ToString()),
                    new Claim("username", user.Username),
                    new Claim("email", user.Email),
                    new Claim("type", user.Type),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                };

                var token = new JwtSecurityToken(
                    issuer: _jwtConfig.Issuer,
                    audience: _jwtConfig.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_jwtConfig.ExpirationMinutes),
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                
                _logger.LogInformation($"JWT token generated successfully for user: {user.Username}");
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating JWT token for user {user.Username}: {ex.Message}", ex);
                throw;
            }
        }

        public bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtConfig.SecretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtConfig.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtConfig.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"JWT token validation failed: {ex.Message}");
                return false;
            }
        }

        public int? GetUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                
                var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "user_id");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting user ID from token: {ex.Message}");
                return null;
            }
        }

        public string? GetUsernameFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                
                var usernameClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "username");
                return usernameClaim?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting username from token: {ex.Message}");
                return null;
            }
        }
    }
}
