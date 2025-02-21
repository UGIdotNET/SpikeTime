using Microsoft.EntityFrameworkCore;

namespace UGIdotNET.SpikeTime.Aspire9.Entities;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = default!;
}
