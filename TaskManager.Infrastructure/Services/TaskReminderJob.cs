using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManager.Domain.Interfaces;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Services;

public class TaskReminderJob : ITaskReminderJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<TaskReminderJob> _logger;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailService _emailService;

    public TaskReminderJob(
        AppDbContext context,
        ILogger<TaskReminderJob> logger,
        UserManager<IdentityUser> userManager,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task CheckOverdueTasksAsync()
    {
        _logger.LogInformation("Starting overdue task check at {Time}", DateTime.UtcNow);

        var newlyOverdueTasks = await _context.Tasks
            .Where(t => !t.IsCompleted
                     && t.DueDate.HasValue
                     && t.DueDate < DateTime.UtcNow
                     && !t.IsOverdue)
            .ToListAsync();

        if (newlyOverdueTasks.Count == 0)
        {
            _logger.LogInformation("No new overdue tasks found");
        }
        else
        {
            foreach (var task in newlyOverdueTasks)
            {
                task.IsOverdue = true;
                task.UpdatedAt = DateTime.UtcNow;

                _logger.LogWarning(
                    "Task marked overdue: Id={TaskId}, Title={Title}, DueDate={DueDate}",
                    task.Id, task.Title, task.DueDate);
            }

            await _context.SaveChangesAsync();

            // ── SEND EMAIL NOTIFICATIONS ─────────────────────
            // Do this AFTER saving to DB — if email sending fails
            // for some reason, the overdue flag is still correctly
            // persisted. Email is a "nice to have" notification,
            // not something that should block the core data update
            foreach (var task in newlyOverdueTasks)
            {
                await NotifyTaskOwnerAsync(task.UserId, task.Title, task.DueDate);
            }
            // ─────────────────────────────────────────────────
        }

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

        _logger.LogInformation("Overdue check complete. Flagged: {NewCount}, Un-flagged: {ClearedCount}",
            newlyOverdueTasks.Count, completedButStillFlagged.Count);
    }

    // ── PRIVATE HELPER ─────────────────────────────────────
    // Looks up the task owner's email and sends the notification
    // Wrapped in try/catch — a failed email must NEVER crash the
    // whole background job or prevent other tasks from being processed
    private async Task NotifyTaskOwnerAsync(string userId, string taskTitle, DateTime? dueDate)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                _logger.LogWarning("Could not find email for UserId={UserId}, skipping notification", userId);
                return;
            }

            var subject = $"Task Overdue: {taskTitle}";
            var htmlBody = $@"
                <h2>Task Overdue</h2>
                <p>Your task <strong>{taskTitle}</strong> was due on {dueDate:MMMM dd, yyyy} and is now overdue.</p>
                <p>Please log in to your Task Manager to update it.</p>";

            await _emailService.SendEmailAsync(user.Email, subject, htmlBody, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // ── FAIL SAFE ──────────────────────────────────
            // Log the error but don't rethrow — one failed email
            // shouldn't stop the rest of the overdue-check job
            _logger.LogError(ex, "Failed to send overdue notification for UserId={UserId}", userId);
        }
    }
    // ─────────────────────────────────────────────────────
}