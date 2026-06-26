using Microsoft.EntityFrameworkCore;

namespace UGIdotNET.SpikeTime.MutationTesting.Data;

public class TodoListDbContext : DbContext
{
    public TodoListDbContext(DbContextOptions<TodoListDbContext> options)
        : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TodoItem>(t =>
        {
            t.HasKey(t => t.Id);

            t.Property(t => t.Title).HasMaxLength(255).IsRequired();
        });
    }
}
