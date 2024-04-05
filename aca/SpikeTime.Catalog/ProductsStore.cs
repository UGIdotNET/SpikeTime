namespace SpikeTime.Catalog;

public static class ProductsStore
{
    public static List<Product> Products { get; } = [
        new() { Id = Guid.NewGuid(), Name = "Mac Book pro", Description = "Mac book di ultima generazione", Price = 2400 },
        new() { Id = Guid.NewGuid(), Name = "Dell XPS", Description = "PC Dell XPS di ultima generazione", Price = 2000 },
        new() { Id = Guid.NewGuid(), Name = "Harry Potter e la pietra filosofale", Description = "Primo libro della saga più amata da grandi e piccini", Price = 15 }
    ];
}

public class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }
}
