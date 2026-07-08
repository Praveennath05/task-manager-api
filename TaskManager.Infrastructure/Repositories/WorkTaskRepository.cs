using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Repositories;

public class WorkTaskRepository : IWorkTaskRepository
{
    private readonly AppDbContext _context;

    public WorkTaskRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── CHANGED — FILTER BY USER IN THE QUERY ──────────────
    // .Where(t => t.UserId == userId) runs as part of the SQL query
    // itself — EF Core translates this to "WHERE UserId = @userId"
    // Only that user's rows ever leave the database
    public async Task<List<WorkTask>> GetAllAsync(string userId, CancellationToken cancellationToken)
    {
        return await _context.Tasks
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);
    }
    // ─────────────────────────────────────────────────────

    public async Task<WorkTask?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Tasks.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<WorkTask> CreateAsync(WorkTask task, CancellationToken cancellationToken)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task<WorkTask> UpdateAsync(WorkTask task, CancellationToken cancellationToken)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var task = await _context.Tasks.FindAsync(new object[] { id }, cancellationToken);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}