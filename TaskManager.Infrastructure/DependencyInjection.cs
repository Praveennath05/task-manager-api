using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Domain.Interfaces;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Repositories;
using TaskManager.Infrastructure.Services;


namespace TaskManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF CORE ──────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                "Server=ASUS;Database=TaskManagerDb;Trusted_Connection=True;TrustServerCertificate=True"));
                Console.WriteLine($"[DEBUG] Redis connection string: {configuration.GetConnectionString("Redis")}");
        // ─────────────────────────────────────────────────────

        // ── ASP.NET CORE IDENTITY ─────────────────────────────
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            // Password rules — relax for development, tighten for production
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();
        // ─────────────────────────────────────────────────────

        // ── DEPENDENCY INJECTION ──────────────────────────────
        services.AddScoped<IWorkTaskRepository, WorkTaskRepository>();

        // Same pattern for Auth:
        services.AddScoped<IAuthService, AuthService>();
        
        // ── TOKEN SERVICE ─────────────────────────────────────
        services.AddScoped<TaskManager.Infrastructure.Services.TokenService>();        
          
        services.AddScoped<ICacheService, CacheService>(); 
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITaskReminderJob, TaskReminderJob>();
        services.AddScoped<IEmailService,EmailService>();
        
           
        
        
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "TaskManager_";
        });
        return services;
    }
}