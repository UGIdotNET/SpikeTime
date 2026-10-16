using Microsoft.AspNetCore.Identity;

namespace VideoHub.Api.Services;

public static class AdminSeeder
{
    public const string AdminRole = "Admin";

    // Creates the single local admin from Admin:Email / Admin:Password (user-secrets or env vars).
    public static async Task SeedAsync(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(AdminSeeder));
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<IdentityUser>>();

        if (!await roles.RoleExistsAsync(AdminRole))
        {
            await roles.CreateAsync(new IdentityRole(AdminRole));
        }

        var email = config["Admin:Email"];
        var password = config["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin:Email / Admin:Password not configured: no admin user seeded.");
            return;
        }

        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin seed failed: " + string.Join("; ", created.Errors.Select(e => e.Description)));
            }
        }

        if (!await users.IsInRoleAsync(user, AdminRole))
        {
            await users.AddToRoleAsync(user, AdminRole);
        }
    }
}
