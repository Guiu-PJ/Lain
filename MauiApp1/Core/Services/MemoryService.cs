using Microsoft.EntityFrameworkCore;
using MiAplicacion.Data;
using MiAplicacion.Data.Models;

public class MemoryService
{
    private readonly AppDbContext _db;

    public MemoryService(AppDbContext db)
    {
        _db = db;
        _db.Database.EnsureCreated();
    }

    public async Task<List<Memory>> GetAllAsync() =>
        await _db.Memories.OrderByDescending(m => m.Importance).ToListAsync();

    public async Task SaveAsync(string content, string? category = null, float importance = 1.0f)
    {
        _db.Memories.Add(new Memory
        {
            Content = content,
            Category = category,
            Importance = importance
        });
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var m = await _db.Memories.FindAsync(id);
        if (m is null) return;
        _db.Memories.Remove(m);
        await _db.SaveChangesAsync();
    }
}