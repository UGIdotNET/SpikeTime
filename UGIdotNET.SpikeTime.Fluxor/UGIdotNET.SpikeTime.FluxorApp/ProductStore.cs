namespace UGIdotNET.SpikeTime.FluxorApp;

public static class ProductStore
{
    public static IEnumerable<Product> ProductsCatalog { get; } =
    [
        new Product { Id = Guid.NewGuid(), Name = "Product 1", Price = 100 },
        new Product { Id = Guid.NewGuid(), Name = "Product 2", Price = 200 },
        new Product { Id = Guid.NewGuid(), Name = "Product 3", Price = 300 },
        new Product { Id = Guid.NewGuid(), Name = "Product 4", Price = 400 },
        new Product { Id = Guid.NewGuid(), Name = "Product 5", Price = 500 },
    ];
}

public class Product
{
    public Guid Id { get; init; }

    public string Name { get; init; }

    public decimal Price { get; init; }
}
