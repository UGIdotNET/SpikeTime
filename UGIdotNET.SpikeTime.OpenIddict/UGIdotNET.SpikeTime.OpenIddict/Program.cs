using Microsoft.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using System.Collections.Immutable;
using System.Security.Claims;
using UGIdotNET.SpikeTime.OpenIddict.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllersWithViews();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SchemePolicy", policy =>
    {
        policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite($"Filename={Path.Combine(Path.GetTempPath(), "spiketime-identity-sqlite")}");

    options.UseOpenIddict();
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        // Configure OpenIddict to use the Entity Framework Core stores/models.
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        // Enable the authorization and token endpoints.
        options.SetTokenEndpointUris("/connect/token");
        
        // Allow client applications to use the authorization code flow.
        options.AllowClientCredentialsFlow();

        // Accept anonymous clients (i.e clients that don't send a client_id).
        //options.AcceptAnonymousClients();

        // Register the signing and encryption credentials.
        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();

        options.UseAspNetCore();
    });


var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");

app
    .MapPost("/connect/token", Exchange)
    .DisableAntiforgery();

app
    .MapGet("/api/message", GetMessage)
    .RequireAuthorization("SchemePolicy");

await SeedClient(app);

app.Run();

async Task SeedClient(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();

    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.EnsureCreatedAsync();

    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    if (await manager.FindByClientIdAsync("console") == null)
    {
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "console",
            ClientSecret = "388D45FA-B36B-4988-BA59-B187D329C207",
            DisplayName = "My client application",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials
            }
        });
    }
}

async Task<IResult> GetMessage(
    HttpContext context,
    IOpenIddictApplicationManager applicationManager)
{
    var subject = context.User.FindFirst(Claims.Subject)?.Value;
    if (string.IsNullOrEmpty(subject))
    {
        return Results.BadRequest();
    }

    var application = await applicationManager.FindByClientIdAsync(subject);
    if (application == null)
    {
        return Results.BadRequest();
    }

    return Results.Content($"{await applicationManager.GetDisplayNameAsync(application)} has been successfully authenticated.");
}

async Task<IResult> Exchange(
    HttpContext context,
    IOpenIddictApplicationManager applicationManager, 
    IOpenIddictScopeManager scopeManager)
{
    var request = context.GetOpenIddictServerRequest()!;
    if (request.IsClientCredentialsGrantType())
    {
        var application = await applicationManager.FindByClientIdAsync(request.ClientId!);
        if (application is null)
        {
            return Results.BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidClient,
                ErrorDescription = "The specified client identifier is invalid.",
                ErrorUri = "https://tools.ietf.org/html/rfc6749#section-2.2"
            });
        }

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, request.ClientId!);
        identity.SetScopes(request.GetScopes());

        var resources = await GetResourcesAsync(scopeManager, identity.GetScopes());
        identity.SetResources(resources);
        identity.SetDestinations(GetDestinations);

        return Results.SignIn(
            new ClaimsPrincipal(identity),
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    return Results.BadRequest(new OpenIddictResponse
    {
        Error = Errors.UnsupportedGrantType,
        ErrorDescription = "The specified grant type is invalid."
    });
}

async Task<ImmutableArray<string>> GetResourcesAsync(
    IOpenIddictScopeManager scopeManager, 
    ImmutableArray<string> scopes)
{
    var result = new List<string>();

    var resources = scopeManager.ListResourcesAsync(scopes);
    await foreach (var resource in resources)
    {
        result.Add(resource);
    }

    return result.ToImmutableArray();
}

IEnumerable<string> GetDestinations(Claim claim)
{
    // Note: by default, claims are NOT automatically included in the access and identity tokens.
    // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
    // whether they should be included in access tokens, in identity tokens or in both.

    return claim.Type switch
    {
        Claims.Name or Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
        _ => [Destinations.AccessToken],
    };
}
