using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using VideoHub.Data;

namespace VideoHub.Api.Tests;

// Boots the real Api against a private in-memory SQLite database (no Docker / PostgreSQL needed).
public class VideoHubApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Adm1n!Test-only";
    public const string UserEmail = "user@test.local";
    public const string UserPassword = "Us3r!Test-only";

    private static int _ipCounter;
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public VideoHubApiFactory()
    {
        _connection.Open();
        // Program.cs reads these while building, before any factory configuration hook runs.
        Environment.SetEnvironmentVariable("ConnectionStrings__videohub", "Host=unused;Database=unused");
        Environment.SetEnvironmentVariable("Admin__Email", AdminEmail);
        Environment.SetEnvironmentVariable("Admin__Password", AdminPassword);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var npgsqlRegistrations = services.Where(d =>
                d.ServiceType == typeof(AppDbContext)
                || d.ServiceType == typeof(DbContextOptions)
                || (d.ServiceType.IsGenericType && d.ServiceType.GenericTypeArguments.Contains(typeof(AppDbContext))))
                .ToList();
            foreach (var d in npgsqlRegistrations)
            {
                services.Remove(d);
            }

            services.AddDbContext<AppDbContext>(o => o
                .UseSqlite(_connection)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .ReplaceService<IModelCustomizer, SqliteDateTimeOffsetModelCustomizer>());
        });
    }

    // Each client gets its own fake client IP so tests never share the per-IP rate limit bucket.
    public HttpClient CreateClientWithIp(string? ip = null, string? voterId = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ip ?? NextIp());
        if (voterId is not null)
        {
            client.DefaultRequestHeaders.Add("X-Voter-Id", voterId);
        }

        return client;
    }

    public static string NextIp()
    {
        var n = Interlocked.Increment(ref _ipCounter);
        return $"10.{n / 250}.{n % 250}.1";
    }

    public static string NewVoterId() => Guid.NewGuid().ToString("N");

    public async Task EnsureNonAdminUserAsync()
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        if (await users.FindByEmailAsync(UserEmail) is not null)
        {
            return;
        }

        var result = await users.CreateAsync(
            new IdentityUser { UserName = UserEmail, Email = UserEmail, EmailConfirmed = true }, UserPassword);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
