using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UGIdotNET.SpikeTime.Aspire9.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<CatalogDbContext>("spiketime-db");

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

app.MapDefaultEndpoints();

app.Run();