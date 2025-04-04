using Microsoft.EntityFrameworkCore;

namespace UGIdotNET.SpikeTime.OpenIddict.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
