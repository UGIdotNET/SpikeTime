using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Endpoints;
using VideoHub.Api.Services;
using VideoHub.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("videohub");

builder.Services.AddIdentityCore<IdentityUser>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

builder.Services.AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme, o => o.BearerTokenExpiration = TimeSpan.FromHours(8));
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AdminSeeder.AdminRole, p => p.RequireRole(AdminSeeder.AdminRole));

builder.Services.AddSingleton<VoterIdentity>();
builder.Services.AddProblemDetails();

// The API sits behind the Blazor frontend inside the Aspire network: trust its X-Forwarded-For.
// Do not expose the API publicly without restricting these proxies.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(RateLimitPolicies.PublicWrites, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy(RateLimitPolicies.Login, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await AdminSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapAuthEndpoints();
app.MapVideoEndpoints();
app.MapRatingEndpoints();
app.MapCommentEndpoints();
app.MapAdminVideoEndpoints();
app.MapAdminModerationEndpoints();

app.Run();

// Exposed for WebApplicationFactory<Program> in VideoHub.Api.Tests.
public partial class Program;
