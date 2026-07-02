using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<WorkTask> Tasks { get; set; }

    // ── REFRESH TOKENS TABLE ───────────────────────────────
    // EF Core creates a "RefreshTokens" table from this DbSet
    // Each row = one issued refresh token for one user session
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    // ─────────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });

        // ── REFRESH TOKEN CONFIGURATION ─────────────────────
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Token must be unique — no two rows can have the same token string
            entity.HasIndex(e => e.Token).IsUnique();

            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired();
        });
        // ─────────────────────────────────────────────────
    }
}