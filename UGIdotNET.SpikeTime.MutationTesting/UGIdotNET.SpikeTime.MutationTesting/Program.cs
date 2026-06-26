using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data = UGIdotNET.SpikeTime.MutationTesting.Data;
using Models = UGIdotNET.SpikeTime.MutationTesting.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<Data.TodoListDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("spiketime-mutations-db")));

builder.Services.AddValidation();

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

var todoItems = app.MapGroup("/api/todos");

todoItems.MapGet("/", GetTodoItems).WithName(nameof(GetTodoItems));
todoItems.MapGet("/{id:guid}", GetTodoItemById).WithName(nameof(GetTodoItemById));
todoItems.MapGet("/completed/count", GetCompletedCountForWeek).WithName(nameof(GetCompletedCountForWeek));
todoItems.MapPost("/", CreateNewTodo).WithName(nameof(CreateNewTodo));
todoItems.MapPut("/{id:guid}", UpdateTodo).WithName(nameof(UpdateTodo));
todoItems.MapPatch("/{id:guid}/complete", MarkTodoAsCompleted).WithName(nameof(MarkTodoAsCompleted));
todoItems.MapDelete("/{id:guid}", DeleteTodo).WithName(nameof(DeleteTodo));

app.MapDefaultEndpoints();

app.Run();

static async Task<Ok<Models.TodoListItem[]>> GetTodoItems(
    Data.TodoListDbContext db)
{
    var items = await db.TodoItems
        .OrderBy(t => t.CompletedAt != null)
        .ThenByDescending(t => t.CreatedAt)
        .Select(t => new Models.TodoListItem(t.Id, t.Title, t.CreatedAt, t.CompletedAt))
        .ToArrayAsync();

    return TypedResults.Ok(items);
}

static async Task<Ok<int>> GetCompletedCountForWeek(
    DateTime date,
    Data.TodoListDbContext db)
{
    var startOfWeek = StartOfWeek(date);
    var endOfWeek = startOfWeek.AddDays(7);

    var count = await db.TodoItems
        .Where(t => t.CompletedAt != null
            && t.CompletedAt >= startOfWeek
            && t.CompletedAt < endOfWeek)
        .CountAsync();

    return TypedResults.Ok(count);
}

// Returns the Monday (00:00) of the week containing the supplied date.
static DateTime StartOfWeek(DateTime date)
{
    var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
    return date.Date.AddDays(-diff);
}

static async Task<Results<Ok<Models.TodoListItem>, NotFound>> GetTodoItemById(
    Guid id,
    Data.TodoListDbContext db)
{
    var item = await db.TodoItems
        .Where(t => t.Id == id)
        .Select(t => new Models.TodoListItem(t.Id, t.Title, t.CreatedAt, t.CompletedAt))
        .FirstOrDefaultAsync();
    return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
}

static async Task<Results<CreatedAtRoute<Models.TodoItem>, ValidationProblem>> CreateNewTodo(
    [FromBody] Models.TodoItem item,
    Data.TodoListDbContext db)
{
    var normalizedTitle = item.Title.Trim();
    if (normalizedTitle.Length > 100)
    {
        normalizedTitle = normalizedTitle[..100];
    }

    var todoItem = new Data.TodoItem
    {
        Id = Guid.NewGuid(),
        Title = normalizedTitle,
        CreatedAt = DateTime.Now
    };
    db.TodoItems.Add(todoItem);
    await db.SaveChangesAsync();

    return TypedResults.CreatedAtRoute(item, nameof(GetTodoItemById), new { id = todoItem.Id });
}

static async Task<Results<NoContent, NotFound>> UpdateTodo(
    Guid id,
    [FromBody] Models.TodoItem item,
    Data.TodoListDbContext db)
{
    var existingItem = await db.TodoItems.FindAsync(id);
    if (existingItem is null)
    {
        return TypedResults.NotFound();
    }

    existingItem.Title = item.Title;
    await db.SaveChangesAsync();
    
    return TypedResults.NoContent();
}

static async Task<Results<NoContent, NotFound>> MarkTodoAsCompleted(
    Guid id,
    Data.TodoListDbContext db)
{
    var item = await db.TodoItems.FindAsync(id);
    if (item is null)
    {
        return TypedResults.NotFound();
    }

    item.MarkAsComplete();
    await db.SaveChangesAsync();

    return TypedResults.NoContent();
}

static async Task<Results<NoContent, NotFound>> DeleteTodo(
    Guid id,
    Data.TodoListDbContext db)
{
    var item = await db.TodoItems.FindAsync(id);
    if (item is null)
    {
        return TypedResults.NotFound();
    }

    db.TodoItems.Remove(item);
    await db.SaveChangesAsync();
    
    return TypedResults.NoContent();
}

