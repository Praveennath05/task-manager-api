using Microsoft.Extensions.Logging;
using TaskManager.Domain.Interfaces;
using TaskManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.Infrastructure.Services;

public class TaskReminderJob : ITaskReminderJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<TaskReminderJob> _logger;

    // ── DEPENDENCY INJECTION ──────────────────────────────
    
    public TaskReminderJob(AppDbContext context, ILogger<TaskReminderJob> logger)
    {
        _context = context;
        _logger = logger;
    }
    // ─────────────────────────────────────────────────────

    public async Task CheckOverdueTasksAsync()
    {
        _logger.LogInformation("Starting overdue task check at {Time}", DateTime.UtcNow);

        // ── QUERY ────────────────────────────────────────
        var overdueTasks = await _context.Tasks
            .Where(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate < DateTime.UtcNow)
            .ToListAsync();

        if (overdueTasks.Count == 0)
        {
            _logger.LogInformation("No overdue tasks found");
            return;
        }

        // ── LOG EACH OVERDUE TASK ─────────────────────────
        foreach (var task in overdueTasks)
        {
            _logger.LogWarning(
                "Task overdue: Id={TaskId}, Title={Title}, DueDate={DueDate}",
                task.Id, task.Title, task.DueDate);
        }

        _logger.LogInformation("Overdue check complete. Found {Count} overdue tasks", overdueTasks.Count);
    }
}