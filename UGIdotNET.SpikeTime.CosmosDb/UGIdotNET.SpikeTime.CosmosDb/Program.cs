using Microsoft.Azure.Cosmos;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var client = new CosmosClient(
    "<YOUR ENDPOINT",
    "<YOUR KEY>");

var databaseResponse = await client.CreateDatabaseIfNotExistsAsync("spiketime");
var database = databaseResponse.Database;

Console.WriteLine($"Database {database.Id} created!");

var containerResponse = await database.CreateContainerIfNotExistsAsync(
    "products", 
    partitionKeyPath: "/categoryId");

var container = containerResponse.Container;

Console.WriteLine($"Container {container.Id} created!");

var category1 = Guid.Parse("2e6e559b-a5e7-4505-bc13-35830be9a155");
var category2 = Guid.Parse("d2c98bfe-6b68-4ebf-b7b2-908ed60d2a95");

await CreateProductsAsync(container, category1, category2);

Console.WriteLine("Read all products");
await ReadAllProductsAsync(container);

Console.WriteLine($"Read products by category {category1}");
await ReadProductsByCategoryAsync(container, category1);

Console.WriteLine($"Read products by category {category2}");
await ReadProductsByCategoryAsync(container, category2);


static async Task ReadProductsByCategoryAsync(Container container, Guid categoryId)
{
    var queryDefinition = new QueryDefinition(
        "SELECT * FROM c WHERE c.categoryId = @categoryId")
        .WithParameter("@categoryId", categoryId);

    var iterator = container.GetItemQueryIterator<Product>(
        queryDefinition);

    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync();

        foreach (var product in response)
        {
            Console.WriteLine($"{product}");
            Console.WriteLine("--------------------");
        }
    }
}
static async Task ReadAllProductsAsync(Container container)
{
    var iterator = container.GetItemQueryIterator<Product>();

    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync();

        foreach (var product in response)
        {
            Console.WriteLine($"{product}");
            Console.WriteLine("--------------------");
        }
    }
}

static async Task CreateProductsAsync(Container container, Guid category1, Guid category2)
{
    for (int i = 0; i < 10; i++)
    {
        var categoryId = i % 2 == 0 ? category1 : category2;

        var product = new Product(
            Guid.NewGuid(),
            categoryId,
            $"Product {i}",
            (decimal)100.00);

        var productResponse = await container.CreateItemAsync(
            product,
            new PartitionKey(categoryId.ToString()));

        Console.WriteLine($"Product {productResponse.Resource.name} created!");
    }
}

record Product(
    Guid id,
    Guid categoryId,
    string name,
    decimal price);
