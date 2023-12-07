using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UGIdotNET.IdentityApi.Blazor.Client.Pages;
using UGIdotNET.IdentityApi.Blazor.Components;
using UGIdotNET.IdentityApi.Blazor.Components.Account;
using UGIdotNET.IdentityApi.Blazor.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddTransient<IClaimsTransformation, SpikeTimeClaimsTransformation>();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Counter).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();


public class SpikeTimeClaimsTransformation : IClaimsTransformation
{
    public SpikeTimeClaimsTransformation(UserManager<ApplicationUser> userManager)
    {
        UserManager = userManager;
    }

    public UserManager<ApplicationUser> UserManager { get; }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var user = await UserManager.FindByNameAsync(principal.Identity!.Name!);

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.GivenName, user!.FirstName));
        identity.AddClaim(new Claim(ClaimTypes.Surname, user.LastName));

        principal.AddIdentity(identity);

        return principal;
    }
}

public static class UserExtensions
{
    public static string GetFullName(this ClaimsPrincipal user)
    {
        var firstName = user.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty;
        var lastName = user.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;

        return $"{firstName} {lastName}";
    }
}
