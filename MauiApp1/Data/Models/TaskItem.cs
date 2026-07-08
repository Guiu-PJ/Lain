namespace MiAplicacion.Data.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Priority Priority { get; set; } = Priority.Normal;
}

public enum Priority
{
    Low,
    Normal,
    High
}