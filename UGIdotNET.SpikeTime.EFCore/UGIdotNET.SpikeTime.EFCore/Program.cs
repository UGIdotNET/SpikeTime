//See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using UGIdotNET.SpikeTime.EFCore;

Console.WriteLine("Hello, Spike time! Ep. 7 - Entity Framework Core");

using var context = new SpikeTimeDbContext();
if (context.Database.GetPendingMigrations().Any())
{
    context.Database.Migrate();
}

var (customer, order) = SeedData(context);
UpdateCustomerAddress(context, customer, order);

AddShow(context);
FindTodayShow(context);

AddHierarcy(context);

for (short i = 0; i < 3; i++)
{
    QueryHierarcy(context, i);
}


void QueryHierarcy(SpikeTimeDbContext context, short level)
{
    var generation = context.Halflings
        .Where(halfling => halfling.PathFromPatriarch.GetLevel() == level)
        .ToList();

    foreach (var item in generation)
    {
        Console.WriteLine($"{item.Name}, {item.PathFromPatriarch}");
    }
}

void AddHierarcy(SpikeTimeDbContext context)
{
    context.AddRange(
    new Halfling(HierarchyId.Parse("/"), "Balbo", 1167),
    new Halfling(HierarchyId.Parse("/1/"), "Mungo", 1207),
    new Halfling(HierarchyId.Parse("/2/"), "Pansy", 1212),
    new Halfling(HierarchyId.Parse("/3/"), "Ponto", 1216),
    new Halfling(HierarchyId.Parse("/4/"), "Largo", 1220),
    new Halfling(HierarchyId.Parse("/5/"), "Lily", 1222),
    new Halfling(HierarchyId.Parse("/1/1/"), "Bungo", 1246),
    new Halfling(HierarchyId.Parse("/1/2/"), "Belba", 1256),
    new Halfling(HierarchyId.Parse("/1/3/"), "Longo", 1260),
    new Halfling(HierarchyId.Parse("/1/4/"), "Linda", 1262),
    new Halfling(HierarchyId.Parse("/1/5/"), "Bingo", 1264),
    new Halfling(HierarchyId.Parse("/3/1/"), "Rosa", 1256),
    new Halfling(HierarchyId.Parse("/3/2/"), "Polo"),
    new Halfling(HierarchyId.Parse("/4/1/"), "Fosco", 1264),
    new Halfling(HierarchyId.Parse("/1/1/1/"), "Bilbo", 1290),
    new Halfling(HierarchyId.Parse("/1/3/1/"), "Otho", 1310),
    new Halfling(HierarchyId.Parse("/1/5/1/"), "Falco", 1303),
    new Halfling(HierarchyId.Parse("/3/2/1/"), "Posco", 1302),
    new Halfling(HierarchyId.Parse("/3/2/2/"), "Prisca", 1306),
    new Halfling(HierarchyId.Parse("/4/1/1/"), "Dora", 1302),
    new Halfling(HierarchyId.Parse("/4/1/2/"), "Drogo", 1308),
    new Halfling(HierarchyId.Parse("/4/1/3/"), "Dudo", 1311),
    new Halfling(HierarchyId.Parse("/1/3/1/1/"), "Lotho", 1310),
    new Halfling(HierarchyId.Parse("/1/5/1/1/"), "Poppy", 1344),
    new Halfling(HierarchyId.Parse("/3/2/1/1/"), "Ponto", 1346),
    new Halfling(HierarchyId.Parse("/3/2/1/2/"), "Porto", 1348),
    new Halfling(HierarchyId.Parse("/3/2/1/3/"), "Peony", 1350),
    new Halfling(HierarchyId.Parse("/4/1/2/1/"), "Frodo", 1368),
    new Halfling(HierarchyId.Parse("/4/1/3/1/"), "Daisy", 1350),
    new Halfling(HierarchyId.Parse("/3/2/1/1/1/"), "Angelica", 1381));

    context.SaveChanges();
}

void FindTodayShow(SpikeTimeDbContext context)
{
    var shows = context.Shows
        .Where(s => s.AirDates.Any(d => d >= DateTime.Today))
        .ToArray();

    foreach (var show in shows)
    {
        Console.WriteLine(show.Title);
        foreach (var date in show.AirDates)
        {
            Console.WriteLine(date);
        }
    }
}

void AddShow(SpikeTimeDbContext context)
{
    var show = new Show
    {
        Title = "Spike time",
        AirDates = [new(2024, 02, 23, 18, 30, 0), new(2024, 03, 08, 18, 30, 0)]
    };

    context.Shows.Add(show);
    context.SaveChanges();
}

void UpdateCustomerAddress(SpikeTimeDbContext context, Customer customer, Order order)
{
    customer.Address = customer.Address with { City = "Brescia", Contact = new("aaa@aaa.it", "22222") };
    context.SaveChanges();
}

(Customer, Order) SeedData(SpikeTimeDbContext context)
{
    if (context.Customers.Any())
    {
        context.Customers.RemoveRange(context.Customers);
        context.SaveChanges();
    }

    var customer = new Customer
    {
        DateOfBirth = new DateOnly(1987, 8, 24),
        FullName = "Alberto Mori",
        Address = new Address
        {
            City = "Montichiari",
            Province = "BS",
            ZipCode = "25018",
            Contact = new("albi.mori@gmail.com", "1111111")
        }
    };

    context.Add(customer);

    var order = new Order
    {
        Contents = "Pizza",
        Customer = customer,
        BillingAddress = customer.Address,
        ShippingAddress = customer.Address
    };
    context.Orders.Add(order);

    context.SaveChanges();

    return (customer, order);
}