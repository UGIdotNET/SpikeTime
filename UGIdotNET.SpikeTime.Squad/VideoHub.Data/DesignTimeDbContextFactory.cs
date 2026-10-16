using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VideoHub.Data;

// Used only by `dotnet ef` tooling; the connection string is never opened when adding migrations.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=videohub;Username=design;Password=design")
            .Options;
        return new AppDbContext(options);
    }
}
