using TaskManager.Domain.Entities;

namespace TaskManager.Domain.Interfaces;

public interface IWorkTaskRepository
{
    // ── CHANGED — FILTER BY USER ───────────────────────
    // Only returns tasks belonging to the specified user
    // Filtering happens in the database query itself (efficient),
    // not by fetching everyone's tasks and filtering in memory
    Task<List<WorkTask>> GetAllAsync(string userId, CancellationToken cancellationToken);
    // ─────────────────────────────────────────────────

    Task<WorkTask?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<WorkTask> CreateAsync(WorkTask task, CancellationToken cancellationToken);
    Task<WorkTask> UpdateAsync(WorkTask task, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}