using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicPortalPolicy", policy =>
        policy.WithOrigins("http://localhost:4321", "http://127.0.0.1:4321")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var connectionString = builder.Configuration.GetConnectionString("videocatalogdb")
    ?? "Host=localhost;Port=5432;Database=videocatalogdb;Username=postgres;Password=postgres";

builder.Services.AddNpgsqlDataSource(connectionString);
builder.Services.AddSingleton<VideoCatalogRepository>();

var app = builder.Build();

app.UseCors("PublicPortalPolicy");
app.MapDefaultEndpoints();

app.MapGet("/", () => Results.Ok(new { message = "Video catalog public API" }));

app.MapGet("/api/public/videos", async (VideoCatalogRepository repository) =>
    Results.Ok(await repository.GetVideosAsync()));

app.MapPost("/api/public/videos/{videoId:int}/ratings", async (int videoId, VoteRequest request, VideoCatalogRepository repository) =>
{
    if (request.Rating is < 1 or > 5)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.Rating)] = ["Il voto deve essere compreso tra 1 e 5."]
        });
    }

    if (!await repository.VideoExistsAsync(videoId))
    {
        return Results.NotFound();
    }

    await repository.AddRatingAsync(videoId, request.Rating);
    return Results.Ok(await repository.GetVideoByIdAsync(videoId));
});

var repository = app.Services.GetRequiredService<VideoCatalogRepository>();
await repository.InitializeAsync();

app.Run();

public record VoteRequest(int Rating);

public sealed class VideoCatalogRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public VideoCatalogRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InitializeAsync()
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS videos (
                id SERIAL PRIMARY KEY,
                title TEXT NOT NULL,
                description TEXT NOT NULL,
                url TEXT NOT NULL,
                platform TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS ratings (
                id SERIAL PRIMARY KEY,
                video_id INTEGER NOT NULL REFERENCES videos(id) ON DELETE CASCADE,
                rating SMALLINT NOT NULL CHECK (rating BETWEEN 1 AND 5),
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );", connection);

        await cmd.ExecuteNonQueryAsync();

        await using var countCmd = new NpgsqlCommand("SELECT COUNT(*) FROM videos;", connection);
        var count = (long)(await countCmd.ExecuteScalarAsync() ?? 0);

        if (count == 0)
        {
            await using var insertCmd = new NpgsqlCommand(@"
                INSERT INTO videos (title, description, url, platform) VALUES
                ('Introduzione a ASP.NET Core', 'Panoramica chiara su architettura, hosting e sviluppi moderni.', 'https://www.youtube.com/watch?v=4G_R6GJm6r8', 'YouTube'),
                ('E-commerce in Blazor', 'Esempio pratico di un pannello amministrativo con gestione contenuti e dashboard.', 'https://www.youtube.com/watch?v=6Y4xt5T42E8', 'YouTube'),
                ('PostgreSQL per applicazioni moderne', 'Come strutturare dati e query in ambienti cloud-first.', 'https://vimeo.com/76979871', 'Vimeo');", connection);
            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<bool> VideoExistsAsync(int videoId)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT 1 FROM videos WHERE id = @id;", connection);
        cmd.Parameters.AddWithValue("id", videoId);
        var result = await cmd.ExecuteScalarAsync();
        return result is not null;
    }

    public async Task AddRatingAsync(int videoId, int rating)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO ratings (video_id, rating) VALUES (@videoId, @rating);",
            connection);

        cmd.Parameters.AddWithValue("videoId", videoId);
        cmd.Parameters.AddWithValue("rating", rating);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<VideoItem>> GetVideosAsync()
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(@"
            SELECT v.id, v.title, v.description, v.url, v.platform,
                   COALESCE(ROUND(AVG(r.rating)::numeric, 2), 0) AS average_rating,
                   COUNT(r.id) AS ratings_count
            FROM videos v
            LEFT JOIN ratings r ON r.video_id = v.id
            GROUP BY v.id, v.title, v.description, v.url, v.platform
            ORDER BY v.id DESC;", connection);

        await using var reader = await cmd.ExecuteReaderAsync();
        var result = new List<VideoItem>();

        while (await reader.ReadAsync())
        {
            result.Add(new VideoItem(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                Convert.ToDouble(reader[5] ?? 0),
                reader.GetInt64(6)));
        }

        return result;
    }

    public async Task<VideoItem?> GetVideoByIdAsync(int videoId)
    {
        var videos = await GetVideosAsync();
        return videos.FirstOrDefault(v => v.Id == videoId);
    }
}

public sealed record VideoItem(
    int Id,
    string Title,
    string Description,
    string Url,
    string Platform,
    double AverageRating,
    long RatingsCount);
