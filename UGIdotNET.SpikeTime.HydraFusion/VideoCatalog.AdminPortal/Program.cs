using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using VideoCatalog.AdminPortal.Components;
using VideoCatalog.AdminPortal.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "videocatalog-admin";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    })
    .AddKeycloakOpenIdConnect(
        serviceName: "keycloak",
        realm: "video-catalog",
        options =>
        {
            options.ClientId = "admin-portal";
            options.ClientSecret = builder.Configuration["Authentication:ClientSecret"]
                ?? throw new InvalidOperationException("Keycloak client secret is not configured.");
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.TokenValidationParameters.NameClaimType = "preferred_username";
            options.TokenValidationParameters.RoleClaimType = "roles";
        });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("VideoAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("video-admin");
    });
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<VideoAdminClient>((_, client) =>
{
    client.BaseAddress = new Uri(builder.Configuration["AdminApiUrl"] ?? "http://localhost:5010");
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/login", (string? returnUrl) =>
    {
        var redirectUri = IsLocalUrl(returnUrl) ? returnUrl! : "/";

        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = redirectUri },
            [OpenIdConnectDefaults.AuthenticationScheme]);
    })
    .AllowAnonymous();

app.MapPost("/logout", async (HttpContext context, IAntiforgery antiforgery) =>
    {
        await antiforgery.ValidateRequestAsync(context);

        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            ]);
    })
    .RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool IsLocalUrl(string? url) =>
    !string.IsNullOrWhiteSpace(url)
    && url[0] == '/'
    && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
