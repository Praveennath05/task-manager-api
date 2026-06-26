using TaskManager.Domain.Entities;

namespace TaskManager.Domain.Interfaces;

public interface IWorkTaskRepository
{
    // ── CANCELLATION TOKEN ────────────────────────────
    // Allows database operations to stop if client disconnects
    Task<List<WorkTask>> GetAllAsync(CancellationToken cancellationToken);
    Task<WorkTask?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<WorkTask> CreateAsync(WorkTask task, CancellationToken cancellationToken);
    Task<WorkTask> UpdateAsync(WorkTask task, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    // ─────────────────────────────────────────────────
}