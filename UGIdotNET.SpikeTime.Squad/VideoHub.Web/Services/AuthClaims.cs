namespace VideoHub.Web.Services;

public static class AuthClaims
{
    // Stored only inside the encrypted, HttpOnly auth cookie; never rendered to the page.
    public const string AccessToken = "videohub:access_token";
}
