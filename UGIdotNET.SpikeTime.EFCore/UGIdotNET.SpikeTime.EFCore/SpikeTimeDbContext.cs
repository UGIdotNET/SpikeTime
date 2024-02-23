using Microsoft.EntityFrameworkCore;

namespace UGIdotNET.SpikeTime.EFCore;

public class SpikeTimeDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; } = default!;

    public DbSet<Order> Orders { get; set; } = default!;

    public DbSet<Show> Shows { get; set; } = default!;

    public DbSet<Halfling> Halflings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            "Server=localhost\\sqlexpress;Database=UGIdotNET-SpikeTime-EFCore;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",
            x => x.UseHierarchyId());

        optionsBuilder
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, [DbLoggerCategory.Database.Command.Name]);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>()
            .ComplexProperty(c => c.Address, b => b.ComplexProperty(a => a.Contact));

        modelBuilder.Entity<Order>(b =>
        {
            b.ComplexProperty(
                o => o.ShippingAddress,
                ab => ab.ComplexProperty(a => a.Contact));
            b.ComplexProperty(
                o => o.BillingAddress,
                ab => ab.ComplexProperty(a => a.Contact));
        });
    }
}

public class Halfling
{
    public Halfling(HierarchyId pathFromPatriarch, string name, int? yearOfBirth = null)
    {
        PathFromPatriarch = pathFromPatriarch;
        Name = name;
        YearOfBirth = yearOfBirth;
    }

    public int Id { get; private set; }
    public HierarchyId PathFromPatriarch { get; set; }
    public string Name { get; set; }
    public int? YearOfBirth { get; set; }
}

public class Customer
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public required Address Address { get; set; }

    public List<Order> Orders { get; set; } = [];
}

public record Address
{
    public string City { get; init; }
    public string Province { get; init; }
    public string ZipCode { get; init; }
    public Contact Contact {  get; init; }
}

public record Contact(
    string Email,
    string PhoneNumber);

public class Order
{
    public int Id { get; set; }
    public required string Contents { get; set; }
    public required Address ShippingAddress { get; set; }
    public required Address BillingAddress { get; set; }
    public Customer Customer { get; set; } = null!;
}

public class Show
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<DateTime> AirDates { get; set; } = [];
}


