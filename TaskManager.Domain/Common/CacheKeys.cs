namespace TaskManager.Domain.Common;

public static class CacheKeys
{
    public const string AllTasks = "tasks:all";
    public static string TaskById(int id) => $"tasks:{id}";
    
}
