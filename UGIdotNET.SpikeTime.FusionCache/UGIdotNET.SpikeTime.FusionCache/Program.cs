using Microsoft.AspNetCore.Mvc;
using UGIdotNET.SpikeTime.FusionCache.Models;
using UGIdotNET.SpikeTime.FusionCache.Services;
using ZiggyCreatures.Caching.Fusion;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddFusionCache("books")
    .WithOptions(options =>
    {
        options.DefaultEntryOptions = new()
        {
            IsFailSafeEnabled = true,
            Duration = TimeSpan.FromSeconds(10),
        };
    });

builder.Services.AddFusionCache("authors");

builder.Services
    .AddScoped<BooksService>()
    .AddScoped<AuthorsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet(
    "/api/books",
    async (BooksService service, IFusionCacheProvider cacheProvider) =>
    {
        var cache = cacheProvider.GetCache("books");

        var books = await cache.GetOrSetAsync(
            "books",
            async _ => await service.GetBooksAsync());

        return Results.Ok(books);
    });

app.MapGet(
    "/api/books/{id:int}",
    async (BooksService service, IFusionCacheProvider cacheProvider, int id) =>
    {
        var cache = cacheProvider.GetCache("books");

        var book = await cache.GetOrSetAsync<Book>(
            $"book:{id}",
            async (ctx, _) => await service.GetBookByIdAsync(id),
            options => options.SetDuration(TimeSpan.FromSeconds(30)));

        return Results.Ok(book);
    });

app.MapPut(
    "/api/books/{id:int}",
    async (BooksService service, IFusionCacheProvider cacheProvider, int id, [FromBody] Book model) =>
    {
        var cache = cacheProvider.GetCache("books");

        await service.UpdateBookAsync(id, model);
        await cache.ExpireAsync($"book:{id}");

        return Results.NoContent();
    });

app.MapGet(
    "/api/authors",
    async (AuthorsService service, IFusionCacheProvider cacheProvider) =>
    {
        var cache = cacheProvider.GetCache("authors");

        var authors = await cache.GetOrSetAsync(
            "authors",
            async _ => await service.GetAuthorsAsync(),
            options => options.SetDuration(TimeSpan.FromSeconds(5)));
        return Results.Ok(authors);
    });

app.MapGet(
    "/api/authors/{id:int}",
    async (AuthorsService service, int id) =>
    {
        var author = await service.GetAuthorByIdAsync(id);
        return Results.Ok(author);
    });

app.Run();