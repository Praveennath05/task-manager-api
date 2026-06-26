using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Repositories;

public class WorkTaskRepository : IWorkTaskRepository
{
    private readonly AppDbContext _context;

    // ── DEPENDENCY INJECTION ──────────────────────────────
    // AppDbContext is injected by .NET automatically
    public WorkTaskRepository(AppDbContext context)
    {
        _context = context;
    }
    // ─────────────────────────────────────────────────────

    public async Task<List<WorkTask>> GetAllAsync(CancellationToken cancellationToken)
    {
        // ── CANCELLATION TOKEN ────────────────────────────
        // Passed to EF Core — stops the DB query if client disconnects
        return await _context.Tasks.ToListAsync(cancellationToken);
        // ─────────────────────────────────────────────────
    }

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