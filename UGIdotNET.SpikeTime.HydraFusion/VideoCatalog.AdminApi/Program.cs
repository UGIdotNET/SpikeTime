using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("videocatalogdb")
    ?? "Host=localhost;Port=5432;Database=videocatalogdb;Username=postgres;Password=postgres";

builder.Services.AddNpgsqlDataSource(connectionString);
builder.Services.AddSingleton<AdminVideoRepository>();

builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: "video-catalog",
        options =>
        {
            options.Audience = "admin-api";
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is not ClaimsIdentity identity)
                    {
                        return Task.CompletedTask;
                    }

                    foreach (var role in context.Principal.FindAll("roles"))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role.Value));
                    }

                    var realmAccess = context.Principal.FindFirst("realm_access")?.Value;
                    if (!string.IsNullOrWhiteSpace(realmAccess))
                    {
                        using var document = JsonDocument.Parse(realmAccess);
                        if (document.RootElement.TryGetProperty("roles", out var roles))
                        {
                            foreach (var role in roles.EnumerateArray())
                            {
                                var value = role.GetString();
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, value));
                                }
                            }
                        }
                    }

                    return Task.CompletedTask;
                }
            };
        });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("VideoAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("video-admin");
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

app.MapGet("/", () => Results.Ok(new { message = "Video catalog admin API" }));

app.MapGet("/api/admin/videos", async (AdminVideoRepository repository) =>
    Results.Ok(await repository.GetVideosAsync()))
    .RequireAuthorization("VideoAdmin");

app.MapPost("/api/admin/videos", async (CreateVideoRequest request, AdminVideoRepository repository) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.Url))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.Title)] = ["Tutti i campi sono obbligatori."],
            [nameof(request.Description)] = ["Tutti i campi sono obbligatori."],
            [nameof(request.Url)] = ["Tutti i campi sono obbligatori."]
        });
    }

    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.Url)] = ["Inserire un link valido a YouTube o a un'altra piattaforma video."]
        });
    }

    var created = await repository.CreateAsync(request.Title.Trim(), request.Description.Trim(), request.Url.Trim());
    return Results.Created($"/api/admin/videos/{created.Id}", created);
}).RequireAuthorization("VideoAdmin");

var repository = app.Services.GetRequiredService<AdminVideoRepository>();
await repository.InitializeAsync();

app.Run();

public record CreateVideoRequest(string Title, string Description, string Url);

public sealed class AdminVideoRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public AdminVideoRepository(NpgsqlDataSource dataSource)
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

    public async Task<VideoItem> CreateAsync(string title, string description, string url)
    {
        var platform = DetectPlatform(url);

        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO videos (title, description, url, platform)
            VALUES (@title, @description, @url, @platform)
            RETURNING id, title, description, url, platform, 0::numeric AS average_rating, 0::bigint AS ratings_count;", connection);

        cmd.Parameters.AddWithValue("title", title);
        cmd.Parameters.AddWithValue("description", description);
        cmd.Parameters.AddWithValue("url", url);
        cmd.Parameters.AddWithValue("platform", platform);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException("Impossibile creare il contenuto video.");
        }

        return new VideoItem(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            0,
            0);
    }

    private static string DetectPlatform(string url)
    {
        var value = url.ToLowerInvariant();
        if (value.Contains("youtube.com") || value.Contains("youtu.be"))
        {
            return "YouTube";
        }

        if (value.Contains("vimeo.com"))
        {
            return "Vimeo";
        }

        if (value.Contains("tiktok.com"))
        {
            return "TikTok";
        }

        return "Altro";
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
