using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VideoHub.Web.Components;
using VideoHub.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/login";
        o.Cookie.Name = "videohub_admin";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.SlidingExpiration = false;
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", p => p.RequireRole("Admin"));

builder.Services.AddTransient<ApiContextHandler>();
builder.Services.AddHttpClient<VideoHubApiClient>(c => c.BaseAddress = new Uri("https+http://api"))
    .AddHttpMessageHandler<ApiContextHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseStaticFiles();
app.UseMiddleware<VoterIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/logout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest();
    }

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapRazorComponents<App>();
app.MapDefaultEndpoints();

app.Run();
