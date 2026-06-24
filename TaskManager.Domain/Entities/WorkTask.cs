using TaskManager.Domain.Common;

namespace TaskManager.Domain.Entities;

public class WorkTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public DateTime? DueDate { get; set; }
}