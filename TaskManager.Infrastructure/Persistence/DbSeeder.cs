using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace TaskManager.Infrastructure.Persistence;

public static class DbSeeder
{
    // ── SEEDER ────────────────────────────────────────────
    // Runs once at app startup
    // Creates Admin and User roles if they don't exist
    // Without this, AddToRoleAsync("User") in AuthService would fail
    // because the role doesn't exist in the database yet
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        // ── ROLE MANAGER ──────────────────────────────────
        // RoleManager is Identity's built-in class for managing roles
        // We get it from the DI container
        var roleManager = serviceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();
        // ─────────────────────────────────────────────────

        // ── CREATE ROLES ──────────────────────────────────
        // Only creates if it doesn't already exist
        // Safe to run every time app starts
        string[] roles = { "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
        // ─────────────────────────────────────────────────
    }
}