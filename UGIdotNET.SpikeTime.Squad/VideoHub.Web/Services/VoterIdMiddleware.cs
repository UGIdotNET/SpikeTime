using System.Security.Cryptography;

namespace VideoHub.Web.Services;

public class VoterIdMiddleware(RequestDelegate next)
{
    public const string CookieName = "vh_web_voter";
    public const string ItemKey = "VoterId";

    public Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Cookies[CookieName];
        if (!IsValid(id))
        {
            id = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace('+', '-').Replace('/', '_');
            context.Response.Cookies.Append(CookieName, id, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
        }

        context.Items[ItemKey] = id;
        return next(context);
    }

    private static bool IsValid(string? id) =>
        id is { Length: >= 16 and <= 64 } && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
