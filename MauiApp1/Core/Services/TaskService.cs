using MiAplicacion.Data;
using MiAplicacion.Data.Models;
using Microsoft.EntityFrameworkCore;

public class TaskService
{
    private readonly AppDbContext _db;

    public TaskService(AppDbContext db)
    {
        _db = db;
        _db.Database.EnsureCreated();
    }

    public async Task<List<TaskItem>> GetAllAsync() =>
        await _db.Tasks.OrderBy(t => t.DueDate).ToListAsync();

    public async Task<List<TaskItem>> GetTodayAsync()
    {
        var today = DateTime.Today;
        return await _db.Tasks
            .Where(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value.Date == today)
            .ToListAsync();
    }

    public async Task AddAsync(TaskItem task)
    {
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
    }

    public async Task CompleteAsync(int id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return;
        task.IsCompleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return;
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
    }
}