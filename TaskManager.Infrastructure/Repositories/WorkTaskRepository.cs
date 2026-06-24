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

    public async Task<List<WorkTask>> GetAllAsync()
    {
        return await _context.Tasks.ToListAsync();
    }

    public async Task<WorkTask?> GetByIdAsync(int id)
    {
        return await _context.Tasks.FindAsync(id);
    }

    public async Task<WorkTask> CreateAsync(WorkTask task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<WorkTask> UpdateAsync(WorkTask task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task DeleteAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }
    }
}
