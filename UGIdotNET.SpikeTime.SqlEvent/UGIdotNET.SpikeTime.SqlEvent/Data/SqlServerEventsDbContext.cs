using Microsoft.EntityFrameworkCore;

namespace UGIdotNET.SpikeTime.SqlEvent.Data;

public class SqlServerEventsDbContext : DbContext
{
    public SqlServerEventsDbContext(DbContextOptions<SqlServerEventsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Order>(o =>
        {
            o.ToTable("Orders");

            o.HasKey(o => o.Id);

            o.Property(o => o.Customer).HasMaxLength(255).IsRequired();
            o.Property(o => o.Product).HasMaxLength(255).IsRequired();

            o.Property(o => o.Status).HasConversion<string?>();
        });
    }
}
