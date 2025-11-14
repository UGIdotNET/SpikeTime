using Microsoft.EntityFrameworkCore;

namespace UGIdotNET.SpikeTime.VectorSql.Data;

public class SpikeTimeDbContext : DbContext
{
    public SpikeTimeDbContext(DbContextOptions<SpikeTimeDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Episode> Episodes { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Episode>(builder =>
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.Title).HasMaxLength(200);

            //TODO configure vector property
            builder.Property(e => e.Embedding).HasColumnType("vector(1536)");
        });
    }
}
