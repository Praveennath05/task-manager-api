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
        // ─────────────────────────────────────────────────────

        // ── ASP.NET CORE IDENTITY ─────────────────────────────
       services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;

    // ── ACCOUNT LOCKOUT ──────────────────────────────
    // Explicit rather than relying on Identity's implicit defaults
    // After 5 failed login attempts, lock the account for 5 minutes
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.AllowedForNewUsers = true;
    // ─────────────────────────────────────────────────
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
        services.AddScoped<IEmailService, EmailService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();




        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "TaskManager_";
        });
        return services;
    }
}