
using Microsoft.EntityFrameworkCore;
using PlatformFlower.Middleware;

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

            // Register services for dependency injection
            builder.Services.AddScoped<PlatformFlower.Services.Common.Response.IResponseService, PlatformFlower.Services.Common.Response.ResponseService>();
            builder.Services.AddScoped<PlatformFlower.Services.Common.Validation.IValidationService, PlatformFlower.Services.Common.Validation.ValidationService>();
            builder.Services.AddScoped<PlatformFlower.Services.Common.Logging.IAppLogger, PlatformFlower.Services.Common.Logging.AppLogger>();
            builder.Services.AddScoped<PlatformFlower.Services.User.IUserService, PlatformFlower.Services.User.UserService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
