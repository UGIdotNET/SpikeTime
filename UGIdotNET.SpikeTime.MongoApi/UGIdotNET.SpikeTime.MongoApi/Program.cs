using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<MongoDbServices>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

MapCollections();

app.MapGet("/api/movies", async (MongoDbServices services) =>
{
    var movies = await services.GetMoviesAsync();
    return Results.Ok(movies);
}).WithOpenApi().WithName("Movies");

app.MapGet("/api/users", async (MongoDbServices services) =>
{
    var users = await services.GetUsersAsync();
    return Results.Ok(users);
}).WithOpenApi().WithName("Users");

app.Run();

static void MapCollections()
{
    BsonClassMap.RegisterClassMap<Movie>(cm =>
    {
        cm.AutoMap();
        cm.SetIgnoreExtraElements(true);
    });

    BsonClassMap.RegisterClassMap<User>(cm =>
    {
        cm.AutoMap();
        cm.SetIgnoreExtraElements(true);
    });
}


public class MongoDbServices
{
    private MongoClient _client;

    private IMongoDatabase _database;

    public MongoDbServices()
    {
        _client = new MongoClient(new MongoUrl("<ConnectionString>"));
        _database = _client.GetDatabase("sample_mflix");
    }

    public async Task<List<Movie>> GetMoviesAsync()
    {
        var collection = _database.GetCollection<Movie>("movies");
        var moviesCollection = await collection.FindAsync(new BsonDocument());
        return moviesCollection.ToList();
    }

    public async Task<List<User>> GetUsersAsync()
    {
        var collection = _database.GetCollection<User>("users");
        var usersCollection = await collection.FindAsync(new BsonDocument());
        return usersCollection.ToList();
    }
}

public record User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("email")]
    public string Email { get; set; }

    [BsonElement("password")]
    public string Password { get; set; }
}

public record Movie
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("plot")]
    public string? Plot { get; set; }
}

public record Imdb
{
    [BsonElement("rating")]
    public double Rating { get; set; }

    [BsonElement("votes")]
    public int Votes { get; set; }

    [BsonElement("id")]
    public int Id { get; set; }
}

public record Award
{
    [BsonElement("wins")]
    public int Wins { get; set; }

    [BsonElement("nominations")]
    public int Nominations { get; set; }

    [BsonElement("text")]
    public string? Text { get; set; }
}
