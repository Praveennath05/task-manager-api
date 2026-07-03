namespace TaskManager.Domain.Interfaces;

public interface ITaskReminderJob
{
    Task CheckOverdueTasksAsync();
}