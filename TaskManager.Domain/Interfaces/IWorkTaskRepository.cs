using TaskManager.Domain.Entities;

namespace TaskManager.Domain.Interfaces;

public interface IWorkTaskRepository
{
    Task<List<WorkTask>> GetAllAsync();
    Task<WorkTask?> GetByIdAsync(int id);
    Task<WorkTask> CreateAsync(WorkTask task);
    Task<WorkTask> UpdateAsync(WorkTask task);
    Task DeleteAsync(int id);
}