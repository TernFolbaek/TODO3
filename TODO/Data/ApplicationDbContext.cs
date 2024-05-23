using Microsoft.EntityFrameworkCore;
using TODO.Models;

namespace TODO.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
            
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>().HasKey(x => x.Id); // Justin waffle
    }

    public DbSet<TodoItem> TodoItems { get; set; }
    public DbSet<User> Users { get; set; }
}