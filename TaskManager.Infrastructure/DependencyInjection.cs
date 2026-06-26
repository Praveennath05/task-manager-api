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
        // Registers AppDbContext with SQL Server
        // Every request gets its own DbContext instance (Scoped)
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                "Server=ASUS;Database=TaskManagerDb;Trusted_Connection=True;TrustServerCertificate=True"));
        // ─────────────────────────────────────────────────────

        // ── ASP.NET CORE IDENTITY ─────────────────────────────
        // Adds user management — register, login, password hashing
        // IdentityUser is the default user class (email, password, roles)
        // AddEntityFrameworkStores tells Identity to use our AppDbContext
        // so user tables are created in our existing database
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
        // Tells .NET: when anyone asks for IWorkTaskRepository,
        // give them a WorkTaskRepository instance
        // Scoped = one instance per HTTP request
        services.AddScoped<IWorkTaskRepository, WorkTaskRepository>();

        // Same pattern for Auth:
        // when anyone asks for IAuthService, give them AuthService
        services.AddScoped<IAuthService, AuthService>();
        // ─────────────────────────────────────────────────────

        return services;
    }
}