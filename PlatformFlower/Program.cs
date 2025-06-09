
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PlatformFlower.Middleware;
using System.Text;

namespace PlatformFlower
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "PlatformFlower API",
                    Version = "v1",
                    Description = "API for PlatformFlower application with JWT Authentication"
                });

                // Add JWT Authentication to Swagger
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
            // Connect to the database using Entity Framework Core
            builder.Services.AddDbContext<FlowershopContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            // Register JWT Configuration
            builder.Services.AddSingleton<PlatformFlower.Services.Common.Configuration.IJwtConfiguration, PlatformFlower.Services.Common.Configuration.JwtConfiguration>();

            // Configure JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLongForPlatformFlower2024!";
            var key = Encoding.UTF8.GetBytes(secretKey);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization();

            // Register services for dependency injection
            builder.Services.AddScoped<PlatformFlower.Services.Common.Response.IResponseService, PlatformFlower.Services.Common.Response.ResponseService>();
            builder.Services.AddScoped<PlatformFlower.Services.Common.Validation.IValidationService, PlatformFlower.Services.Common.Validation.ValidationService>();
            builder.Services.AddScoped<PlatformFlower.Services.Common.Logging.IAppLogger, PlatformFlower.Services.Common.Logging.AppLogger>();
            // Register User services (Clean & Simple architecture)
            // Only 2 core services - no facade needed
            builder.Services.AddScoped<PlatformFlower.Services.User.Auth.IAuthService, PlatformFlower.Services.User.Auth.AuthServiceSimple>();
            builder.Services.AddScoped<PlatformFlower.Services.User.Profile.IProfileService, PlatformFlower.Services.User.Profile.ProfileServiceSimple>();
            builder.Services.AddScoped<PlatformFlower.Services.Auth.IJwtService, PlatformFlower.Services.Auth.JwtService>();

            // Register Email services
            builder.Services.AddSingleton<PlatformFlower.Services.Email.IEmailConfiguration, PlatformFlower.Services.Email.EmailConfiguration>();
            builder.Services.AddScoped<PlatformFlower.Services.Email.IEmailService, PlatformFlower.Services.Email.EmailService>();

            // Register Cloudinary services
            builder.Services.AddSingleton<PlatformFlower.Services.Common.Configuration.ICloudinaryConfiguration, PlatformFlower.Services.Common.Configuration.CloudinaryConfiguration>();
            builder.Services.AddScoped<PlatformFlower.Services.Storage.IStorageService, PlatformFlower.Services.Storage.CloudinaryStorageService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            // Health check endpoint for Docker
            app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

            app.Run();
        }
    }
}
