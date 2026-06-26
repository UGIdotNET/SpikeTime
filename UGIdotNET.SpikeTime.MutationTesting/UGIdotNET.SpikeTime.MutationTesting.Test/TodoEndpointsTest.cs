using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UGIdotNET.SpikeTime.MutationTesting.Data;
using UGIdotNET.SpikeTime.MutationTesting.Models;

namespace UGIdotNET.SpikeTime.MutationTesting.Test;

public class TodoEndpointsTest(DatabaseContainerService databaseContainerService) : IClassFixture<DatabaseContainerService>, IAsyncLifetime
{
    private readonly Guid todoItemId = Guid.NewGuid();

    #region Setup WebApplicationFactory
    private Task<WebApplicationFactory<Program>> CreateWebApplicationFactoryAsync()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    databaseContainerService.ReplaceSqlServerConfiguration(services);
                });
            });

        return Task.FromResult(factory);
    }
    #endregion

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<TodoListDbContext>()
            .UseSqlServer(databaseContainerService.SqlServerConnectionString)
            .Options;

        using var db = new TodoListDbContext(options);
        await db.TodoItems.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAllTodos_Should_Return_All_Items()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        await InitializeTodoItemsAsync(app);
        
        using var client = app.CreateClient();

        var response = await client.GetAsync("api/todos");
        var todoItems = await response.Content.ReadFromJsonAsync<TodoListItem[]>() ?? [];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        Assert.Single(todoItems);
        Assert.Equal(todoItemId, todoItems[0].Id);
        Assert.Equal("test 1", todoItems[0].Title);
    }

    [Fact]
    public async Task GetTodoItemById_Should_Return_Item_When_Exists()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        await InitializeTodoItemsAsync(app);
        
        using var client = app.CreateClient();

        var response = await client.GetAsync($"api/todos/{todoItemId}");
        var todoItem = await response.Content.ReadFromJsonAsync<TodoListItem>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(todoItem);
        Assert.Equal(todoItemId, todoItem.Id);
        Assert.Equal("test 1", todoItem.Title);
    }

    [Fact]
    public async Task GetTodoItemById_Should_Return_NotFound_When_Item_Does_Not_Exist()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        using var client = app.CreateClient();

        var nonExistentId = Guid.NewGuid();
        var response = await client.GetAsync($"api/todos/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateNewTodo_Should_Create_And_Return_CreatedAtRoute()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        using var client = app.CreateClient();

        var newTodo = new Models.TodoItem("New Test Todo");
        var response = await client.PostAsJsonAsync("api/todos", newTodo);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var createdTodo = await response.Content.ReadFromJsonAsync<Models.TodoItem>();
        Assert.NotNull(createdTodo);
        Assert.Equal("New Test Todo", createdTodo.Title);
    }

    [Fact]
    public async Task CreateNewTodo_Should_Persist_In_Database()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        using var client = app.CreateClient();

        var newTodo = new Models.TodoItem("Persistent Todo");
        var createResponse = await client.PostAsJsonAsync("api/todos", newTodo);
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<Models.TodoItem>();

        // Verify the todo was persisted
        var locationHeaderValue = createResponse.Headers.Location;

        var getResponse = await client.GetAsync(locationHeaderValue);
        var retrievedTodo = await getResponse.Content.ReadFromJsonAsync<TodoListItem>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(retrievedTodo);
        Assert.Equal("Persistent Todo", retrievedTodo.Title);
    }

    [Fact]
    public async Task UpdateTodo_Should_Update_Existing_Item()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        await InitializeTodoItemsAsync(app);
        
        using var client = app.CreateClient();

        var updatedTodo = new Models.TodoItem("Updated Test Todo");
        var response = await client.PutAsJsonAsync($"api/todos/{todoItemId}", updatedTodo);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the update
        var getResponse = await client.GetAsync($"api/todos/{todoItemId}");
        var retrievedTodo = await getResponse.Content.ReadFromJsonAsync<TodoListItem>();

        Assert.NotNull(retrievedTodo);
        Assert.Equal("Updated Test Todo", retrievedTodo.Title);
    }

    [Fact]
    public async Task UpdateTodo_Should_Return_NotFound_For_NonExistent_Item()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        using var client = app.CreateClient();

        var nonExistentId = Guid.NewGuid();
        var updatedTodo = new Models.TodoItem("Updated Title");
        var response = await client.PutAsJsonAsync($"api/todos/{nonExistentId}", updatedTodo);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkTodoAsCompleted_Should_Set_CompletedAt()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        await InitializeTodoItemsAsync(app);
        
        using var client = app.CreateClient();

        var response = await client.PatchAsync($"api/todos/{todoItemId}/complete", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the item is marked as completed
        var getResponse = await client.GetAsync($"api/todos/{todoItemId}");
        var completedTodo = await getResponse.Content.ReadFromJsonAsync<TodoListItem>();

        Assert.NotNull(completedTodo);
        Assert.NotNull(completedTodo.CompletedAt);
    }

    [Fact]
    public async Task MarkTodoAsCompleted_Should_Return_NotFound_For_NonExistent_Item()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        using var client = app.CreateClient();

        var nonExistentId = Guid.NewGuid();
        var response = await client.PatchAsync($"api/todos/{nonExistentId}/complete", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_Should_Remove_Item()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        await InitializeTodoItemsAsync(app);
        
        using var client = app.CreateClient();

        var response = await client.DeleteAsync($"api/todos/{todoItemId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the item is deleted
        var getResponse = await client.GetAsync($"api/todos/{todoItemId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_Should_Return_NotFound_For_NonExistent_Item()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        using var client = app.CreateClient();

        var nonExistentId = Guid.NewGuid();
        var response = await client.DeleteAsync($"api/todos/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllTodos_Should_Return_Empty_Array_When_No_Items()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        using var client = app.CreateClient();

        var response = await client.GetAsync("api/todos");
        var todoItems = await response.Content.ReadFromJsonAsync<TodoListItem[]>() ?? [];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(todoItems);
    }

    [Fact]
    public async Task GetAllTodos_Should_Return_Pending_First_Newest_First_And_Completed_Last()
    {
        var olderPendingId = Guid.NewGuid();
        var newerPendingId = Guid.NewGuid();
        var completedId = Guid.NewGuid();

        using var app = await CreateWebApplicationFactoryAsync();

        Data.TodoItem[] startingTodoItems = [
            new() { Id = olderPendingId, Title = "older pending", CreatedAt = DateTime.Now.AddDays(-3) },
            new() { Id = newerPendingId, Title = "newer pending", CreatedAt = DateTime.Now.AddDays(-1) },
            new() { Id = completedId, Title = "completed", CreatedAt = DateTime.Now.AddDays(-2), CompletedAt = DateTime.Now },
        ];

        using (var scope = app.Services.CreateScope())
        {
            using var db = scope.ServiceProvider.GetRequiredService<TodoListDbContext>();
            db.TodoItems.AddRange(startingTodoItems);
            await db.SaveChangesAsync();
        }

        using var client = app.CreateClient();

        var response = await client.GetAsync("api/todos");
        var todoItems = await response.Content.ReadFromJsonAsync<TodoListItem[]>() ?? [];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, todoItems.Length);

        // Pending items come first, newest CreatedAt first
        Assert.Equal(newerPendingId, todoItems[0].Id);
        Assert.Equal(olderPendingId, todoItems[1].Id);
        // Completed items are pushed to the end
        Assert.Equal(completedId, todoItems[2].Id);
    }

    [Fact]
    public async Task CreateNewTodo_Should_Trim_Whitespace_From_Title()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        using var client = app.CreateClient();

        var newTodo = new Models.TodoItem("   Padded Title   ");
        var createResponse = await client.PostAsJsonAsync("api/todos", newTodo);

        var getResponse = await client.GetAsync(createResponse.Headers.Location);
        var retrievedTodo = await getResponse.Content.ReadFromJsonAsync<TodoListItem>();

        Assert.NotNull(retrievedTodo);
        Assert.Equal("Padded Title", retrievedTodo.Title);
    }

    [Fact]
    public async Task CreateNewTodo_Should_Truncate_Title_Longer_Than_100_Characters()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        using var client = app.CreateClient();

        var longTitle = new string('a', 150);
        var newTodo = new Models.TodoItem(longTitle);
        var createResponse = await client.PostAsJsonAsync("api/todos", newTodo);

        var getResponse = await client.GetAsync(createResponse.Headers.Location);
        var retrievedTodo = await getResponse.Content.ReadFromJsonAsync<TodoListItem>();

        Assert.NotNull(retrievedTodo);
        Assert.Equal(100, retrievedTodo.Title.Length);
    }

    [Fact]
    public async Task CreateNewTodo_Should_Keep_Title_With_Exactly_100_Characters()
    {
        using var app = await CreateWebApplicationFactoryAsync();
        using var client = app.CreateClient();

        var exactTitle = new string('b', 100);
        var newTodo = new Models.TodoItem(exactTitle);
        var createResponse = await client.PostAsJsonAsync("api/todos", newTodo);

        var getResponse = await client.GetAsync(createResponse.Headers.Location);
        var retrievedTodo = await getResponse.Content.ReadFromJsonAsync<TodoListItem>();

        Assert.NotNull(retrievedTodo);
        Assert.Equal(exactTitle, retrievedTodo.Title);
    }

    // Naive happy-path test: only checks a single mid-week scenario.
    // It deliberately ignores the boundary edge cases (Monday 00:00 start,
    // the exclusive end-of-week instant, adjacent weeks and pending items),
    // so Stryker will report surviving mutants on the week-range logic.
    [Fact]
    public async Task GetCompletedCountForWeek_Should_Return_Completed_Count_For_The_Week()
    {
        using var app = await CreateWebApplicationFactoryAsync();

        var midWeek = new DateTime(2026, 6, 24, 12, 0, 0);

        Data.TodoItem[] startingTodoItems = [
            new() { Id = Guid.NewGuid(), Title = "done a", CreatedAt = midWeek.AddDays(-1), CompletedAt = midWeek },
            new() { Id = Guid.NewGuid(), Title = "done b", CreatedAt = midWeek.AddDays(-1), CompletedAt = midWeek },
        ];

        using (var scope = app.Services.CreateScope())
        {
            using var db = scope.ServiceProvider.GetRequiredService<TodoListDbContext>();
            db.TodoItems.AddRange(startingTodoItems);
            await db.SaveChangesAsync();
        }

        using var client = app.CreateClient();

        var response = await client.GetAsync("api/todos/completed/count?date=2026-06-24");
        var count = await response.Content.ReadFromJsonAsync<int>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, count);
    }

    private async Task InitializeTodoItemsAsync(WebApplicationFactory<Program> app)
    {
        Data.TodoItem[] startingTodoItems = [
            new(){ Id = todoItemId, Title = "test 1", CreatedAt = DateTime.Now },
        ];

        using var scope = app.Services.CreateScope();

        using var db = scope.ServiceProvider.GetRequiredService<TodoListDbContext>();
        db.TodoItems.AddRange(startingTodoItems);

        await db.SaveChangesAsync();
    }
}