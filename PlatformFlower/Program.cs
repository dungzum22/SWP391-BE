
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
            builder.Services.AddSwaggerGen();
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
            builder.Services.AddScoped<PlatformFlower.Services.User.IUserService, PlatformFlower.Services.User.UserService>();
            builder.Services.AddScoped<PlatformFlower.Services.Auth.IJwtService, PlatformFlower.Services.Auth.JwtService>();

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

            app.Run();
        }
    }
}
