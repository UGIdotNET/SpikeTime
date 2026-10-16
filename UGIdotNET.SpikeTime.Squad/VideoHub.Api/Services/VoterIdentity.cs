using System.Text.RegularExpressions;

namespace VideoHub.Api.Services;

public record VoterKeys(string VoterHash, string IpHash, string? NewCookieValue);

public partial class VoterIdentity(IConfiguration configuration, IHostEnvironment env)
{
    public const string CookieName = "vh_voter";
    public const string HeaderName = "X-Voter-Id";

    private readonly string _salt = configuration["Voting:HashSalt"]
        ?? (env.IsDevelopment() ? "dev-only-salt" : throw new InvalidOperationException("Voting:HashSalt must be configured."));

    // The voter id comes from the X-Voter-Id header (set by a trusted frontend) or from our own cookie.
    // The client IP is the connection IP after forwarded headers have been applied.
    public VoterKeys Resolve(HttpContext ctx)
    {
        var id = ctx.Request.Headers[HeaderName].FirstOrDefault();
        if (id is null || !ValidId().IsMatch(id))
        {
            id = ctx.Request.Cookies[CookieName];
        }

        string? created = null;
        if (id is null || !ValidId().IsMatch(id))
        {
            id = created = Guid.NewGuid().ToString("N");
        }

        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return new VoterKeys(Validation.Hash(_salt, "voter:" + id), Validation.Hash(_salt, "ip:" + ip), created);
    }

    public static void SetCookie(HttpContext ctx, string value) =>
        ctx.Response.Cookies.Append(CookieName, value, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = ctx.Request.IsHttps,
            MaxAge = TimeSpan.FromDays(365)
        });

    [GeneratedRegex("^[A-Za-z0-9_-]{16,64}$")]
    private static partial Regex ValidId();
}
