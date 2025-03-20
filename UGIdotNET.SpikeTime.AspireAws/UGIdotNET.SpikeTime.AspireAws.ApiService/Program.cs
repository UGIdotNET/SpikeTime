using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.AccessControl;
using UGIdotNET.SpikeTime.AspireAws.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddSqlServerDbContext<CatalogDbContext>("spiketime-db");

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/products", (CatalogDbContext catalog) =>
{
    var products = catalog.Products.AsNoTracking().ToArray();
    return Results.Ok(products);
});

app.MapGet("/api/products/{id}", (CatalogDbContext catalog, Guid id) =>
{
    var product = catalog.Products.AsNoTracking().FirstOrDefault(p => p.Id == id);
    if (product is null)
    {
        return Results.NotFound();
    }
    return Results.Ok(product);
});

app.MapPost("/api/products", async (CatalogDbContext catalog, [FromBody] Product product) =>
{
    catalog.Products.Add(product);
    await catalog.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
});

app.MapPost(
    "/api/products/{id}/image",
    async (CatalogDbContext catalog, IConfiguration configuration, Guid id, [FromForm] IFormFile image) =>
    {
        //AWS:Resources:spiketime-bucket:BucketName
        var s3Client = new AmazonS3Client(new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.EUWest1,
            Profile = new(configuration["AWS_PROFILE"])
        });

        var product = catalog.Products.Find(id);
        if (product is null)
        {
            return Results.NotFound();
        }

        var request = new PutObjectRequest
        {
            BucketName = configuration["AWS:Resources:spiketime-bucket:BucketName"],
            Key = $"{id}.jpg",
            InputStream = image.OpenReadStream(),
        };

        await s3Client.PutObjectAsync(request);

        // Save the image to the file system or a blob storage
        return Results.NoContent();
    }).DisableAntiforgery();

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
