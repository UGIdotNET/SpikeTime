using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using UGIdotNET.SpikeTime.AspireAzure.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<CatalogDbContext>("spiketime-db");

builder.AddAzureBlobClient("spiketime-blobs");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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
    async (CatalogDbContext catalog, BlobServiceClient client, Guid id, [FromForm] IFormFile image) =>
    {
        var product = catalog.Products.Find(id);
        if (product is null)
        {
            return Results.NotFound();
        }

        using var fileStream = image.OpenReadStream();
        var container = client.GetBlobContainerClient("products");
        await container.CreateIfNotExistsAsync();

        var blob = container.GetBlobClient($"{id}-{image.Name}.jpg");
        await blob.UploadAsync(fileStream, true);

        // Save the image to the file system or a blob storage
        return Results.NoContent();
    }).DisableAntiforgery();

app.MapDefaultEndpoints();

app.Run();
