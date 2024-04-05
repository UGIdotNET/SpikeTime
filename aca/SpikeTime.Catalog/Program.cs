using SpikeTime.Catalog;
using SpikeTime.CatalogModels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/products", () =>
{
    var products = ProductsStore.Products
        .Select(p => new ProductListItem(p.Id, p.Name, p.Price));

    return Results.Ok(products);
});

app.MapGet("/api/products/{id:guid}", (Guid id) =>
{
    var product = ProductsStore.Products
        .SingleOrDefault(p => p.Id == id);

    if (product is null)
    {
        return Results.NotFound();
    }

    var model = new ProductDetail(
        product.Id,
        product.Name,
        product.Price,
        product.Description);

    return Results.Ok(product);
});

app.Run();
