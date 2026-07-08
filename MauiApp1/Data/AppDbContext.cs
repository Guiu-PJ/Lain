using Microsoft.EntityFrameworkCore;
using MiAplicacion.Data.Models;
using System.Collections.Generic;

namespace MiAplicacion.Data;

public class AppDbContext : DbContext
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Memory> Memories => Set<Memory>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}