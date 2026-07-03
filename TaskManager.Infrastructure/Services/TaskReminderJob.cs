using Microsoft.Extensions.Logging;
using TaskManager.Domain.Interfaces;
using TaskManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.Infrastructure.Services;

public class TaskReminderJob : ITaskReminderJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<TaskReminderJob> _logger;

    public TaskReminderJob(AppDbContext context, ILogger<TaskReminderJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CheckOverdueTasksAsync()
    {
        _logger.LogInformation("Starting overdue task check at {Time}", DateTime.UtcNow);

        // ── QUERY — TASKS THAT SHOULD BE FLAGGED ────────────
        // Not completed, has a due date, due date has passed,
        // and NOT ALREADY flagged (avoids re-processing every day)
        var newlyOverdueTasks = await _context.Tasks
            .Where(t => !t.IsCompleted
                     && t.DueDate.HasValue
                     && t.DueDate < DateTime.UtcNow
                     && !t.IsOverdue)
            .ToListAsync();
        // ─────────────────────────────────────────────────

        if (newlyOverdueTasks.Count == 0)
        {
            _logger.LogInformation("No new overdue tasks found");
        }
        else
        {
            // ── MARK AS OVERDUE ─────────────────────────────
            foreach (var task in newlyOverdueTasks)
            {
                task.IsOverdue = true;
                task.UpdatedAt = DateTime.UtcNow;

                _logger.LogWarning(
                    "Task marked overdue: Id={TaskId}, Title={Title}, DueDate={DueDate}",
                    task.Id, task.Title, task.DueDate);
            }

            // ── SAVE TO DATABASE ────────────────────────────
            await _context.SaveChangesAsync();
            // ─────────────────────────────────────────────
        }
        // ─────────────────────────────────────────────────

        // ── UN-FLAG COMPLETED TASKS ──────────────────────────
        var completedButStillFlagged = await _context.Tasks
            .Where(t => t.IsCompleted && t.IsOverdue)
            .ToListAsync();

        if (completedButStillFlagged.Count > 0)
        {
            foreach (var task in completedButStillFlagged)
            {
                task.IsOverdue = false;
                task.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Un-flagged {Count} completed tasks that were previously overdue",
                completedButStillFlagged.Count);
        }
        // ─────────────────────────────────────────────────

        _logger.LogInformation("Overdue check complete. Flagged: {NewCount}, Un-flagged: {ClearedCount}",
            newlyOverdueTasks.Count, completedButStillFlagged.Count);
    }
}