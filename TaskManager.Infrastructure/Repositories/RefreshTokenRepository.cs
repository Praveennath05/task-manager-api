using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)
    {
        // ── LOOKUP BY TOKEN STRING ──────────────────────────
        // Uses the unique index we created earlier — fast lookup
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
        // ─────────────────────────────────────────────────
    }

    public async Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        refreshToken.IsRevoked = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}