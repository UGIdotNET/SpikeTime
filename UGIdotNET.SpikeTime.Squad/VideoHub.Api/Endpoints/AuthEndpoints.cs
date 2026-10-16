using Microsoft.AspNetCore.Identity;
using VideoHub.Api.Contracts;

namespace VideoHub.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Same flow as Identity API's /login: the bearer scheme writes the access-token JSON response.
        app.MapPost("/api/auth/login", async (LoginRequest body, UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn) =>
        {
            if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrEmpty(body.Password))
            {
                return Results.Unauthorized();
            }

            var user = await users.FindByEmailAsync(body.Email);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            signIn.AuthenticationScheme = IdentityConstants.BearerScheme;
            var result = await signIn.PasswordSignInAsync(user, body.Password, isPersistent: false, lockoutOnFailure: true);
            return result.Succeeded ? TypedResults.Empty : Results.Unauthorized();
        }).RequireRateLimiting(RateLimitPolicies.Login);
    }
}
